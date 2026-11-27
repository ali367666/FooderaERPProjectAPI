using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Application.Reports.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Queries.Receipts;

/// <summary>One paid receipt of the list (no order lines; the full receipt is read by its id).</summary>
public class ReceiptListItem
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? ReceiptNumber { get; set; }
    public int RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public string? TableName { get; set; }
    public string? WaiterName { get; set; }
    public DateTime PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class ReceiptListResponse
{
    /// <summary>The days the list covers (calendar days of the business day, both inclusive).</summary>
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    /// <summary>The exact range the list covers (clock time): the days, or the time range inside them when times were given. End exclusive.</summary>
    public DateTime FromDateTime { get; set; }
    public DateTime ToDateTime { get; set; }
    public int Count { get; set; }
    public decimal Total { get; set; }
    public List<ReceiptListItem> Items { get; set; } = new();
}

/// <summary>
/// The old receipts of the POS: paid orders of a period - today, this week, this month, a date range or one date.
/// The days follow the company's business day (midnight when it works by the day, 06:00 in shifts).
/// </summary>
/// <remarks>
/// <paramref name="FromTime"/> / <paramref name="ToTime"/> (or a time carried by <paramref name="From"/> / <paramref name="To"/>) narrow the days to
/// a time range: from FromTime on the first day to the end of the ToTime minute on the last day.
/// </remarks>
public record GetReceiptsQuery(
    ReportPeriod? Period, DateTime? From, DateTime? To, int? RestaurantId, TimeSpan? FromTime = null, TimeSpan? ToTime = null) : IRequest<ReceiptListResponse>;

public class GetReceiptsQueryHandler : IRequestHandler<GetReceiptsQuery, ReceiptListResponse>
{
    private readonly IOrderRepository _orders;
    private readonly ICompanySettingsRepository _settings;
    private readonly ICurrentUserService _currentUser;

    public GetReceiptsQueryHandler(IOrderRepository orders, ICompanySettingsRepository settings, ICurrentUserService currentUser)
    {
        _orders = orders;
        _settings = settings;
        _currentUser = currentUser;
    }

    public async Task<ReceiptListResponse> Handle(GetReceiptsQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var settings = await _settings.GetByCompanyIdAsync(companyId, ct);
        // No period and no date: today.
        var period = query.Period ?? (query.From is null && query.To is null ? ReportPeriod.Today : ReportPeriod.Custom);
        var window = ReportPeriodResolver.Resolve(
            new BusinessReportRequest { Period = period, From = query.From ?? query.To, To = query.To ?? query.From },
            BusinessTime.Now,
            ReportPeriodResolver.DayStartFor(settings));
        window = ReportPeriodResolver.WithTimes(window, query.FromTime, query.ToTime, query.From, query.To);

        var items = await _orders.GetReceiptsAsync(companyId, query.RestaurantId, window.FromUtc, window.ToUtc, ct);
        return new ReceiptListResponse
        {
            FromDate = (window.FromLocal).Date,
            ToDate = (window.ToLocal.AddTicks(-1)).Date,
            FromDateTime = window.FromLocal,
            ToDateTime = window.ToLocal,
            Count = items.Count,
            Total = items.Sum(i => i.TotalAmount),
            Items = items,
        };
    }
}
