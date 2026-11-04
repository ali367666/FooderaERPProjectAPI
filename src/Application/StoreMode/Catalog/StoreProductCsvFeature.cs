using System.Globalization;
using System.Text;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Catalog;

public class StoreProductImportResult
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>All products as CSV (UTF-8, ";" separated, so Excel opens it directly).</summary>
public record ExportStoreProductsQuery : IRequest<string>;

/// <summary>Creates or updates products from CSV; a row is matched by SKU, then barcode. Bad rows are reported and skipped.</summary>
public record ImportStoreProductsCommand(string Csv) : IRequest<StoreProductImportResult>;

public class StoreProductCsvHandlers :
    IRequestHandler<ExportStoreProductsQuery, string>,
    IRequestHandler<ImportStoreProductsCommand, StoreProductImportResult>
{
    public const string Header = "Ad;SKU;Barkod;Kateqoriya;Brend;Vahid;Alış qiyməti;Satış qiyməti;Topdan qiymət;ƏDV %;Minimum stok;IMEI/serial;Aktiv";

    private static readonly Dictionary<string, UnitOfMeasure> Units = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ədəd"] = UnitOfMeasure.Piece, ["piece"] = UnitOfMeasure.Piece, ["kq"] = UnitOfMeasure.Kg, ["kg"] = UnitOfMeasure.Kg,
        ["qram"] = UnitOfMeasure.Gram, ["g"] = UnitOfMeasure.Gram, ["litr"] = UnitOfMeasure.Liter, ["l"] = UnitOfMeasure.Liter,
        ["ml"] = UnitOfMeasure.Ml,
    };

    private readonly IStoreRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public StoreProductCsvHandlers(IStoreRepository repository, ICurrentUserService currentUser, IMediator mediator)
    {
        _repository = repository;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    private static string UnitLabel(UnitOfMeasure u) => u switch
    {
        UnitOfMeasure.Kg => "kq", UnitOfMeasure.Gram => "qram", UnitOfMeasure.Liter => "litr", UnitOfMeasure.Ml => "ml", _ => "ədəd",
    };

    private static string Cell(string? v)
    {
        v ??= string.Empty;
        // a cell starting with = + - @ would run as a formula when opened in Excel
        if (v.Length > 0 && "=+-@".Contains(v[0]) && !decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) v = "'" + v;
        return v.Contains(';') || v.Contains('"') || v.Contains('\n') ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
    }

    private static string Num(decimal? v) => v?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;

    public async Task<string> Handle(ExportStoreProductsQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var categories = (await _repository.GetCategoriesAsync(companyId, ct)).ToDictionary(c => c.Id, c => c.Name);
        var sb = new StringBuilder().AppendLine(Header);

        var skip = 0;
        while (true)
        {
            var (items, _) = await _repository.SearchProductsAsync(companyId, null, null, null, skip, 500, ct);
            if (items.Count == 0) break;
            foreach (var p in items)
            {
                var primary = p.Barcodes.OrderByDescending(b => b.IsPrimary).Select(b => b.Barcode);
                sb.AppendLine(string.Join(';', new[]
                {
                    Cell(p.Name), Cell(p.Sku), Cell(string.Join('|', primary)),
                    Cell(p.CategoryId is { } c && categories.TryGetValue(c, out var cn) ? cn : null),
                    Cell(p.Brand), UnitLabel(p.Unit), Num(p.PurchasePrice), Num(p.SalePrice), Num(p.WholesalePrice),
                    Num(p.VatPercent), Num(p.MinimumStock), p.TracksSerial ? "1" : "0", p.IsActive ? "1" : "0",
                }));
            }
            skip += items.Count;
            if (items.Count < 500) break;
        }
        return sb.ToString();
    }

    public async Task<StoreProductImportResult> Handle(ImportStoreProductsCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var result = new StoreProductImportResult();
        var categories = (await _repository.GetCategoriesAsync(companyId, ct))
            .ToDictionary(c => c.Name.Trim(), c => c.Id, StringComparer.OrdinalIgnoreCase);

        var rows = ParseCsv(command.Csv.TrimStart('﻿'));
        for (var i = 1; i < rows.Count; i++)
        {
            var line = i + 1;
            var r = rows[i];
            if (r.All(string.IsNullOrWhiteSpace)) continue;
            try
            {
                string At(int n) => n < r.Count ? r[n].Trim() : string.Empty;
                decimal? Dec(int n, string what)
                {
                    var t = At(n);
                    if (t.Length == 0) return null;
                    if (!decimal.TryParse(t.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                        throw new FormatException($"{what} rəqəm deyil: \"{t}\"");
                    return d;
                }

                int? categoryId = null;
                if (At(3).Length > 0)
                {
                    if (!categories.TryGetValue(At(3), out var cid))
                        throw new FormatException($"Kateqoriya tapılmadı: \"{At(3)}\" (əvvəlcə yaradın)");
                    categoryId = cid;
                }

                var unit = UnitOfMeasure.Piece;
                if (At(5).Length > 0 && !Units.TryGetValue(At(5), out unit))
                    throw new FormatException($"Vahid tanınmadı: \"{At(5)}\"");

                var codes = At(2).Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                var sku = At(1).Length > 0 ? At(1) : null;

                // existing product: by SKU, then by the first barcode
                Domain.Entities.StoreMode.StoreProduct? existing = null;
                if (sku is not null) existing = await _repository.FindProductByCodeAsync(companyId, sku, ct);
                if (existing is null && codes.Count > 0) existing = await _repository.FindProductByCodeAsync(companyId, codes[0], ct);

                var request = new SaveStoreProductRequest
                {
                    Name = At(0), Sku = sku, Barcodes = codes, CategoryId = categoryId, Brand = At(4).Length > 0 ? At(4) : null,
                    Unit = unit, PurchasePrice = Dec(6, "Alış qiyməti") ?? 0, SalePrice = Dec(7, "Satış qiyməti") ?? 0,
                    WholesalePrice = Dec(8, "Topdan qiymət"), VatPercent = Dec(9, "ƏDV"), MinimumStock = Dec(10, "Minimum stok") ?? 0,
                    TracksSerial = At(11) is "1" or "true" or "bəli" or "Bəli", IsActive = At(12) is not ("0" or "false" or "xeyr" or "Xeyr"),
                };

                // the CSV has no campaign columns: an update keeps the product's running campaign
                if (existing is not null)
                {
                    request.CampaignPrice = existing.CampaignPrice;
                    request.CampaignFromUtc = existing.CampaignFromUtc;
                    request.CampaignToUtc = existing.CampaignToUtc;
                    request.EarnsBonus = existing.EarnsBonus;
                }

                await _mediator.Send(new SaveStoreProductCommand(existing?.Id, request), ct);
                if (existing is null) result.Created++; else result.Updated++;
            }
            catch (Exception ex) when (ex is FormatException or Application.Common.Exceptions.BadRequestException)
            {
                result.Errors.Add($"Sətir {line}: {ex.Message}");
            }
        }
        return result;
    }

    /// <summary>Minimal CSV reader: ";" or "," separated, quotes may hold separators, quotes and line breaks.</summary>
    private static List<List<string>> ParseCsv(string text)
    {
        var sep = text.Split('\n', 2)[0].Count(c => c == ';') >= text.Split('\n', 2)[0].Count(c => c == ',') ? ';' : ',';
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else cell.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == sep) { row.Add(cell.ToString()); cell.Clear(); }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(cell.ToString()); cell.Clear();
                rows.Add(row); row = new List<string>();
            }
            else cell.Append(c);
        }
        if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
        return rows;
    }
}
