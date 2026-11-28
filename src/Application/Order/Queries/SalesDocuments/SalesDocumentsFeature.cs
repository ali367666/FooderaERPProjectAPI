using System.Globalization;
using System.Text;
using Application.Analytics;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Application.Reports.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Queries.SalesDocuments;

public class SalesDocumentsRequest
{
    /// <summary>First day (inclusive); empty = from the beginning. A time inside it narrows the first day.</summary>
    public DateTime? From { get; set; }
    /// <summary>Last day (inclusive); empty = the first day alone, or up to now without From.</summary>
    public DateTime? To { get; set; }
    public TimeSpan? FromTime { get; set; }
    public TimeSpan? ToTime { get; set; }
    /// <summary>Paid or Cancelled; empty = both.</summary>
    public string? Status { get; set; }
    /// <summary>Receipt number, order number or table name.</summary>
    public string? Search { get; set; }
    public int? RestaurantId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class SalesDocumentRow
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = default!;
    public string Number { get; set; } = default!;
    public string OrderNumber { get; set; } = default!;
    /// <summary>Paid time, else when it was closed or opened.</summary>
    public DateTime DateUtc { get; set; }
    public int TableId { get; set; }
    public string TableName { get; set; } = default!;
    public string WaiterName { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal CashAmount { get; set; }
    public decimal CardAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal ReturnedAmount { get; set; }
    /// <summary>Paid or Cancelled.</summary>
    public string Status { get; set; } = default!;
    public string? ProcessedByUserName { get; set; }
}

/// <summary>The cards above the list: they follow the period, branch and search, not the status filter.</summary>
public class SalesDocumentsSummary
{
    public decimal TotalSales { get; set; }
    public int ReceiptCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal TotalDiscount { get; set; }
    /// <summary>Null without a start date or without earlier sales.</summary>
    public decimal? SalesChangePercent { get; set; }
}

public class SalesDocumentsResponse
{
    public SalesDocumentsSummary Summary { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<SalesDocumentRow> Items { get; set; } = new();
}

public record SalesDocumentFilter(
    int CompanyId, DateTime? FromUtc, DateTime? ToUtcExclusive, string? Status, string? Search, int? RestaurantId);

public static class SalesDocumentStatus
{
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";
}

public record GetSalesDocumentsQuery(SalesDocumentsRequest Request) : IRequest<SalesDocumentsResponse>;

/// <summary>The same list as a CSV file (Excel opens it directly).</summary>
public record ExportSalesDocumentsQuery(SalesDocumentsRequest Request) : IRequest<byte[]>;

public class SalesDocumentsHandler :
    IRequestHandler<GetSalesDocumentsQuery, SalesDocumentsResponse>,
    IRequestHandler<ExportSalesDocumentsQuery, byte[]>
{
    private const int MaxPageSize = 200;
    private const int MaxExportRows = 20000;

    private readonly ISalesDocumentRepository _documents;
    private readonly ICompanySettingsRepository _settings;
    private readonly ICurrentUserService _currentUser;

    public SalesDocumentsHandler(
        ISalesDocumentRepository documents, ICompanySettingsRepository settings, ICurrentUserService currentUser)
    {
        _documents = documents;
        _settings = settings;
        _currentUser = currentUser;
    }

    public async Task<SalesDocumentsResponse> Handle(GetSalesDocumentsQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var filter = await BuildFilterAsync(request, ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var summary = await _documents.GetSummaryAsync(filter, ct);
        if (filter.FromUtc is { } from)
        {
            var now = DateTime.UtcNow;
            var previous = SalesReportMath.PreviousWindow(from, filter.ToUtcExclusive ?? now, now);
            var before = await _documents.GetSummaryAsync(filter with { FromUtc = previous.From, ToUtcExclusive = previous.To }, ct);
            summary.SalesChangePercent = SalesReportMath.Change(summary.TotalSales, before.TotalSales);
        }

        var (items, total) = await _documents.GetPageAsync(filter, (page - 1) * pageSize, pageSize, ct);
        return new SalesDocumentsResponse { Summary = summary, Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<byte[]> Handle(ExportSalesDocumentsQuery query, CancellationToken ct)
    {
        var filter = await BuildFilterAsync(query.Request, ct);
        var (items, _) = await _documents.GetPageAsync(filter, 0, MaxExportRows, ct);

        var csv = new StringBuilder();
        csv.AppendLine("Çap nömrəsi;Tarix;Saat;Filial;Masa;Ofisiant;Toplam;Endirim;Endirim %;Nağd;Kart;Borc;Qaytarılıb;Status");
        foreach (var r in items)
        {
            var local = BusinessTime.FromUtc(r.DateUtc);
            csv.AppendLine(string.Join(';',
                Text(r.Number), local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                local.ToString("HH:mm", CultureInfo.InvariantCulture), Text(r.RestaurantName), Text(r.TableName), Text(r.WaiterName),
                Number(r.TotalAmount), Number(r.DiscountAmount), Number(r.DiscountPercent), Number(r.CashAmount), Number(r.CardAmount),
                Number(r.CreditAmount), Number(r.ReturnedAmount), r.Status == SalesDocumentStatus.Paid ? "Ödənilib" : "Ləğv edilib"));
        }

        var encoding = new UTF8Encoding(true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(csv.ToString())).ToArray();
    }

    private static string Number(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>A cell a spreadsheet cannot read as a formula or as two columns.</summary>
    private static string Text(string? value)
    {
        var text = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
        if (text.Length > 0 && "=+-@\t".Contains(text[0]))
            text = "'" + text;
        return text.Contains(';') || text.Contains('"') ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
    }

    private async Task<SalesDocumentFilter> BuildFilterAsync(SalesDocumentsRequest request, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dayStart = ReportPeriodResolver.DayStartFor(await _settings.GetByCompanyIdAsync(companyId, ct));
        DateTime? fromUtc = null, toUtc = null;

        if (request.From is not null || request.To is not null)
        {
            // Only an end date = everything up to that day.
            var window = ReportPeriodResolver.Resolve(
                new BusinessReportRequest { Period = ReportPeriod.Custom, From = request.From ?? request.To, To = request.To },
                BusinessTime.Now, dayStart);
            window = ReportPeriodResolver.WithTimes(window, request.FromTime, request.ToTime, request.From, request.To);
            fromUtc = request.From is null ? null : window.FromUtc;
            toUtc = window.ToUtc;
        }

        var status = string.Equals(request.Status, SalesDocumentStatus.Paid, StringComparison.OrdinalIgnoreCase) ? SalesDocumentStatus.Paid
            : string.Equals(request.Status, SalesDocumentStatus.Cancelled, StringComparison.OrdinalIgnoreCase) ? SalesDocumentStatus.Cancelled
            : null;

        return new SalesDocumentFilter(
            companyId, fromUtc, toUtc, status,
            string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            request.RestaurantId is > 0 ? request.RestaurantId : null);
    }
}
