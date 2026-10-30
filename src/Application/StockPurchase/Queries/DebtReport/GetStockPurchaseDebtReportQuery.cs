using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Application.StockPurchase.Dtos.Response;
using Domain.Enums;
using MediatR;

namespace Application.StockPurchase.Queries.DebtReport;

/// <param name="From">First purchase day (inclusive); null = no lower bound.</param>
/// <param name="To">Last purchase day (inclusive); null = no upper bound.</param>
/// <param name="OnlyOpen">True: only purchases that still owe something.</param>
public record GetStockPurchaseDebtReportQuery(DateTime? From, DateTime? To, int? CounterpartyId, bool OnlyOpen)
    : IRequest<StockPurchaseDebtReportResponse>;

/// <summary>
/// The goods-received costs as a report: per approved purchase what it cost, what was handed back, who paid
/// what and when, and what is still owed - plus totals overall and per supplier.
/// </summary>
public class StockPurchaseDebtReportResponse
{
    public decimal TotalAzn { get; set; }
    public decimal ReturnedAzn { get; set; }
    public decimal NetTotalAzn { get; set; }
    public decimal PaidAzn { get; set; }
    public decimal RemainingAzn { get; set; }
    public decimal OverpaidAzn { get; set; }

    public List<StockPurchaseDebtSupplierRow> Suppliers { get; set; } = new();
    public List<StockPurchaseDebtRow> Purchases { get; set; } = new();
}

public class StockPurchaseDebtSupplierRow
{
    public int CounterpartyId { get; set; }
    public string CounterpartyName { get; set; } = default!;
    public int PurchaseCount { get; set; }
    public decimal NetTotalAzn { get; set; }
    public decimal PaidAzn { get; set; }
    public decimal RemainingAzn { get; set; }
    public decimal OverpaidAzn { get; set; }
}

public class StockPurchaseDebtRow
{
    public int PurchaseId { get; set; }
    public string DocumentNo { get; set; } = default!;
    public string? SupplierInvoiceNo { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public int CounterpartyId { get; set; }
    public string CounterpartyName { get; set; } = default!;
    public string WarehouseName { get; set; } = default!;

    /// <summary>Who recorded the purchase.</summary>
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }

    public decimal TotalAzn { get; set; }
    public decimal ReturnedAzn { get; set; }
    public decimal NetTotalAzn { get; set; }
    public decimal PaidAzn { get; set; }
    public decimal RemainingAzn { get; set; }
    public decimal OverpaidAzn { get; set; }

    public List<StockPurchasePaymentResponse> Payments { get; set; } = new();
    public List<StockPurchaseReturnResponse> Returns { get; set; } = new();
}

public class GetStockPurchaseDebtReportQueryHandler
    : IRequestHandler<GetStockPurchaseDebtReportQuery, StockPurchaseDebtReportResponse>
{
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetStockPurchaseDebtReportQueryHandler(
        IStockPurchaseRepository purchaseRepository,
        ICurrentUserService currentUserService)
    {
        _purchaseRepository = purchaseRepository;
        _currentUserService = currentUserService;
    }

    public async Task<StockPurchaseDebtReportResponse> Handle(
        GetStockPurchaseDebtReportQuery query, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        // Days follow the same business-day cut-off as the other reports.
        DateTime? fromUtc = query.From is { } f ? BusinessTime.ToUtc(f.Date + ReportPeriodResolver.DayStart) : null;
        DateTime? toUtc = query.To is { } t ? BusinessTime.ToUtc(t.Date.AddDays(1) + ReportPeriodResolver.DayStart) : null;

        var purchases = (await _purchaseRepository.GetAllAsync(cancellationToken))
            .Where(p => p.CompanyId == companyId
                && p.Status == StockPurchaseStatus.Completed
                && (fromUtc == null || p.PurchaseDate.ToUniversalTime() >= fromUtc)
                && (toUtc == null || p.PurchaseDate.ToUniversalTime() < toUtc)
                && (query.CounterpartyId == null || p.CounterpartyId == query.CounterpartyId))
            .OrderByDescending(p => p.PurchaseDate)
            .ToList();

        var userNames = await _purchaseRepository.GetUserNamesAsync(
            purchases.SelectMany(p => p.Payments.Select(x => x.CreatedByUserId)
                    .Concat(p.Returns.Select(r => r.CreatedByUserId))
                    .Append(p.CreatedByUserId))
                .OfType<int>(),
            cancellationToken);

        var rows = purchases
            .Select(p => (Purchase: p, Response: StockPurchaseMapper.ToResponse(p, userNames)))
            .Where(x => !query.OnlyOpen || x.Response.RemainingAzn > 0)
            .Select(x => new StockPurchaseDebtRow
            {
                PurchaseId = x.Purchase.Id,
                DocumentNo = x.Purchase.DocumentNo,
                SupplierInvoiceNo = x.Purchase.SupplierInvoiceNo,
                PurchaseDate = x.Purchase.PurchaseDate,
                ApprovedAtUtc = x.Purchase.ApprovedAtUtc,
                CounterpartyId = x.Purchase.CounterpartyId,
                CounterpartyName = x.Response.CounterpartyName,
                WarehouseName = x.Response.WarehouseName,
                CreatedByUserId = x.Response.CreatedByUserId,
                CreatedByUserName = x.Response.CreatedByUserName,
                TotalAzn = x.Response.TotalAzn,
                ReturnedAzn = x.Response.ReturnedAzn,
                NetTotalAzn = x.Response.NetTotalAzn,
                PaidAzn = x.Response.PaidAmountAzn,
                RemainingAzn = x.Response.RemainingAzn,
                OverpaidAzn = x.Response.OverpaidAzn,
                Payments = x.Response.Payments,
                Returns = x.Response.Returns,
            })
            .ToList();

        return new StockPurchaseDebtReportResponse
        {
            TotalAzn = rows.Sum(r => r.TotalAzn),
            ReturnedAzn = rows.Sum(r => r.ReturnedAzn),
            NetTotalAzn = rows.Sum(r => r.NetTotalAzn),
            PaidAzn = rows.Sum(r => r.PaidAzn),
            RemainingAzn = rows.Sum(r => r.RemainingAzn),
            OverpaidAzn = rows.Sum(r => r.OverpaidAzn),
            Suppliers = rows
                .GroupBy(r => (r.CounterpartyId, r.CounterpartyName))
                .Select(g => new StockPurchaseDebtSupplierRow
                {
                    CounterpartyId = g.Key.CounterpartyId,
                    CounterpartyName = g.Key.CounterpartyName,
                    PurchaseCount = g.Count(),
                    NetTotalAzn = g.Sum(r => r.NetTotalAzn),
                    PaidAzn = g.Sum(r => r.PaidAzn),
                    RemainingAzn = g.Sum(r => r.RemainingAzn),
                    OverpaidAzn = g.Sum(r => r.OverpaidAzn),
                })
                .OrderByDescending(s => s.RemainingAzn)
                .ToList(),
            Purchases = rows,
        };
    }
}
