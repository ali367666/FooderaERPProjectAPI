using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Reports;

/// <summary>
/// The goods flow (mal axını): for every product in every warehouse the stock at the start of the period, what each kind of movement did to it
/// (purchase, sale, returns, transfers, adjustments, waste, own use) and the stock at the end - in pieces and in cost value - from the movement ledger.
/// </summary>
public record GetStoreProductFlowQuery(DateTime From, DateTime To, int? WarehouseId, int? CategoryId, int? ProductId) : IRequest<StoreProductFlowResponse>;

/// <summary>The movement lines of one product behind a flow row.</summary>
public record GetStoreProductFlowLinesQuery(int ProductId, DateTime From, DateTime To, int? WarehouseId) : IRequest<List<StoreProductFlowLineResponse>>;

public class StoreProductFlowFigure
{
    /// <summary>Signed: plus grew the stock, minus took it down.</summary>
    public decimal Qty { get; set; }
    public decimal Value { get; set; }
}

public class StoreProductFlowRowResponse
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string? Barcode { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public StoreProductFlowFigure Opening { get; set; } = new();

    /// <summary>Keyed by <see cref="StoreProductFlowHandler.Buckets"/>: purchase, sale, saleReturn, purchaseReturn, transferIn, transferOut, adjustUp, adjustDown, waste, internalUse.</summary>
    public Dictionary<string, StoreProductFlowFigure> Moves { get; set; } = new();

    public StoreProductFlowFigure Closing { get; set; } = new();
}

public class StoreProductFlowResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public StoreProductFlowRowResponse Total { get; set; } = new();
    public List<StoreProductFlowRowResponse> Rows { get; set; } = new();
}

public class StoreProductFlowLineResponse
{
    public DateTime CreatedAtUtc { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public string Bucket { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Value { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentNo { get; set; }
    public string? Reason { get; set; }
}

public class StoreProductFlowHandler :
    IRequestHandler<GetStoreProductFlowQuery, StoreProductFlowResponse>,
    IRequestHandler<GetStoreProductFlowLinesQuery, List<StoreProductFlowLineResponse>>
{
    /// <summary>The kinds of movement shown as columns, in order.</summary>
    public static readonly string[] Buckets =
    {
        "purchase", "saleReturn", "transferIn", "adjustUp", "sale", "purchaseReturn", "transferOut", "adjustDown", "waste", "internalUse",
    };

    private readonly IStoreReportRepository _reports;
    private readonly ICurrentUserService _currentUser;
    private readonly Application.StoreMode.Access.StoreAccessService _access;

    public StoreProductFlowHandler(IStoreReportRepository reports, ICurrentUserService currentUser, Application.StoreMode.Access.StoreAccessService access)
    {
        _reports = reports;
        _currentUser = currentUser;
        _access = access;
    }

    private static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    private static decimal R3(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);

    /// <summary>The column a movement kind belongs to.</summary>
    public static string BucketOf(StoreStockMovementType type) => type switch
    {
        StoreStockMovementType.Purchase => "purchase",
        StoreStockMovementType.Sale => "sale",
        StoreStockMovementType.SaleReturn or StoreStockMovementType.SaleCancel => "saleReturn",
        StoreStockMovementType.PurchaseReturn => "purchaseReturn",
        StoreStockMovementType.TransferIn => "transferIn",
        StoreStockMovementType.TransferOut => "transferOut",
        StoreStockMovementType.AdjustmentIncrease => "adjustUp",
        StoreStockMovementType.AdjustmentDecrease => "adjustDown",
        StoreStockMovementType.Waste => "waste",
        StoreStockMovementType.InternalUse => "internalUse",
        _ => "adjustUp",
    };

    private static StoreProductFlowRowResponse Empty() => new()
    {
        Moves = Buckets.ToDictionary(b => b, _ => new StoreProductFlowFigure()),
    };

    private static void Close(StoreProductFlowRowResponse row)
    {
        row.Closing.Qty = R3(row.Opening.Qty + row.Moves.Values.Sum(m => m.Qty));
        row.Closing.Value = R2(row.Opening.Value + row.Moves.Values.Sum(m => m.Value));
    }

    public async Task<StoreProductFlowResponse> Handle(GetStoreProductFlowQuery q, CancellationToken ct)
    {
        var fromUtc = ReportPeriodResolver.StoreFromUtc(q.From);
        var toUtc = ReportPeriodResolver.StoreToUtc(q.To);
        if (q.WarehouseId is { } wid) await _access.EnsureWarehouseAsync(wid, ct);
        var allowed = await _access.AllowedWarehousesAsync(ct);

        var flat = await _reports.GetProductFlowAsync(_currentUser.CompanyId, fromUtc, toUtc, q.WarehouseId, allowed?.ToList(), q.CategoryId, q.ProductId, ct);

        var rows = flat.GroupBy(r => (r.WarehouseId, r.ProductId)).Select(g =>
        {
            var first = g.First();
            var row = Empty();
            row.WarehouseId = first.WarehouseId;
            row.WarehouseName = first.WarehouseName;
            row.ProductId = first.ProductId;
            row.ProductName = first.ProductName;
            row.Barcode = first.Barcode;
            row.CategoryId = first.CategoryId;
            row.CategoryName = first.CategoryName;
            foreach (var r in g)
            {
                row.Opening.Qty += r.OpeningQty;
                row.Opening.Value += r.OpeningValue;
                var move = row.Moves[BucketOf(r.Type)];
                move.Qty += r.PeriodQty;
                move.Value += r.PeriodValue;
            }
            row.Opening.Qty = R3(row.Opening.Qty);
            row.Opening.Value = R2(row.Opening.Value);
            foreach (var m in row.Moves.Values) { m.Qty = R3(m.Qty); m.Value = R2(m.Value); }
            Close(row);
            return row;
        })
        .OrderBy(r => r.WarehouseName, StringComparer.CurrentCultureIgnoreCase).ThenBy(r => r.ProductName, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

        var total = Empty();
        foreach (var r in rows)
        {
            total.Opening.Qty += r.Opening.Qty;
            total.Opening.Value += r.Opening.Value;
            foreach (var b in Buckets)
            {
                total.Moves[b].Qty += r.Moves[b].Qty;
                total.Moves[b].Value += r.Moves[b].Value;
            }
        }
        total.Opening.Qty = R3(total.Opening.Qty);
        total.Opening.Value = R2(total.Opening.Value);
        foreach (var m in total.Moves.Values) { m.Qty = R3(m.Qty); m.Value = R2(m.Value); }
        Close(total);

        return new StoreProductFlowResponse { From = q.From.Date, To = q.To.Date, Total = total, Rows = rows };
    }

    public async Task<List<StoreProductFlowLineResponse>> Handle(GetStoreProductFlowLinesQuery q, CancellationToken ct)
    {
        var fromUtc = ReportPeriodResolver.StoreFromUtc(q.From);
        var toUtc = ReportPeriodResolver.StoreToUtc(q.To);
        if (q.WarehouseId is { } wid) await _access.EnsureWarehouseAsync(wid, ct);
        var allowed = await _access.AllowedWarehousesAsync(ct);

        var lines = await _reports.GetProductMovementLinesAsync(_currentUser.CompanyId, q.ProductId, fromUtc, toUtc, q.WarehouseId, allowed?.ToList(), ct);
        return lines.Select(l => new StoreProductFlowLineResponse
        {
            CreatedAtUtc = l.CreatedAtUtc,
            WarehouseId = l.WarehouseId,
            WarehouseName = l.WarehouseName,
            Bucket = BucketOf(l.Type),
            Quantity = l.Quantity,
            UnitCost = l.UnitCost,
            Value = R2(l.Quantity * l.UnitCost),
            BalanceAfter = l.BalanceAfter,
            DocumentType = l.DocumentType,
            DocumentNo = l.DocumentNo,
            Reason = l.Reason,
        }).ToList();
    }
}
