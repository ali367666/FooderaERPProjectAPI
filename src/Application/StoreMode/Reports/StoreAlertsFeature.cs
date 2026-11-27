using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.StoreMode.Trade;
using Domain.Constants;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Reports;

public class StoreAlertResponse
{
    /// <summary>LowStock, ShiftOpenTooLong, SupplierOverdue, CustomerOverdue or TransferStuck.</summary>
    public string Kind { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;

    /// <summary>info, warning or danger.</summary>
    public string Severity { get; set; } = "warning";

    /// <summary>The screen to open for it, e.g. /store/stock.</summary>
    public string? Link { get; set; }
}

/// <summary>What needs attention right now, worked out from live data (nothing is stored): low stock, shifts left open, overdue debts, stuck transfers.</summary>
public record GetStoreAlertsQuery : IRequest<List<StoreAlertResponse>>;

public class StoreAlertsHandler : IRequestHandler<GetStoreAlertsQuery, List<StoreAlertResponse>>
{
    private const int ShiftOpenHours = 12;
    private const int TransferStuckDays = 3;

    private readonly IStoreRepository _store;
    private readonly IStoreWarehouseOpsRepository _ops;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public StoreAlertsHandler(IStoreRepository store, IStoreWarehouseOpsRepository ops, IMediator mediator, ICurrentUserService currentUser)
    {
        _store = store;
        _ops = ops;
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public async Task<List<StoreAlertResponse>> Handle(GetStoreAlertsQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var alerts = new List<StoreAlertResponse>();

        if (_currentUser.HasPermission(AppPermissions.StoreStockView))
        {
            var low = (await _store.SearchBalancesAsync(companyId, null, null, true, ct)).Where(b => b.StoreProduct.MinimumStock > 0).ToList();
            if (low.Count > 0)
                alerts.Add(new StoreAlertResponse
                {
                    Kind = "LowStock",
                    Title = "Minimum stokdan aşağı",
                    Message = $"{low.Count} məhsul minimum stokdadır: {string.Join(", ", low.Take(3).Select(b => b.StoreProduct.Name))}{(low.Count > 3 ? "…" : "")}",
                    Severity = "warning",
                    Link = "/store/stock",
                });
        }

        if (_currentUser.HasPermission(AppPermissions.StoreReportView))
        {
            var cutoff = DateTime.UtcNow.AddHours(-ShiftOpenHours);
            var stale = (await _store.SearchShiftsAsync(companyId, null, null, null, ct))
                .Where(s => s.Status == StoreShiftStatus.Open && s.OpenedAtUtc < cutoff).ToList();
            if (stale.Count > 0)
                alerts.Add(new StoreAlertResponse
                {
                    Kind = "ShiftOpenTooLong",
                    Title = "Növbə uzun müddət açıq qalıb",
                    Message = $"{stale.Count} növbə {ShiftOpenHours} saatdan çoxdur açıqdır ({string.Join(", ", stale.Take(3).Select(s => s.StoreCashRegister?.Name ?? "kassa"))}).",
                    Severity = "warning",
                    Link = "/store/shifts",
                });
        }

        if (_currentUser.HasPermission(AppPermissions.StoreSupplierView))
        {
            var debts = await _mediator.Send(new GetStoreSupplierDebtReportQuery(), ct);
            var overdue = debts.Suppliers.Where(s => s.TotalOverdue > 0).ToList();
            if (overdue.Count > 0)
                alerts.Add(new StoreAlertResponse
                {
                    Kind = "SupplierOverdue",
                    Title = "Təchizatçı borcunun vaxtı keçib",
                    Message = $"{overdue.Count} təchizatçıya cəmi {debts.TotalOverdue:0.00} AZN gecikmiş borc var.",
                    Severity = "danger",
                    Link = "/store/suppliers",
                });
        }

        if (_currentUser.HasPermission(AppPermissions.StoreReportFinance))
        {
            var customers = await _mediator.Send(new GetStoreCustomerDebtReportQuery(), ct);
            if (customers.TotalOver30 > 0)
                alerts.Add(new StoreAlertResponse
                {
                    Kind = "CustomerOverdue",
                    Title = "Müştəri borcu gecikir",
                    Message = $"30 gündən köhnə müştəri borcu cəmi {customers.TotalOver30:0.00} AZN.",
                    Severity = "warning",
                    Link = "/store/reports",
                });
        }

        if (_currentUser.HasPermission(AppPermissions.StoreTransferView))
        {
            var cutoff = DateTime.UtcNow.AddDays(-TransferStuckDays);
            var stuck = (await _ops.SearchTransfersAsync(companyId, null, null, null, null, ct))
                .Where(t => t.Status is StoreTransferStatus.Shipped or StoreTransferStatus.PartiallyReceived && (t.ShippedAtUtc ?? t.CreatedAtUtc) < cutoff)
                .ToList();
            if (stuck.Count > 0)
                alerts.Add(new StoreAlertResponse
                {
                    Kind = "TransferStuck",
                    Title = "Transfer qəbul edilməyib",
                    Message = $"{stuck.Count} transfer {TransferStuckDays} gündən çoxdur yoldadır ({string.Join(", ", stuck.Take(3).Select(t => t.DocumentNo))}).",
                    Severity = "warning",
                    Link = "/store/transfers",
                });
        }

        return alerts;
    }
}
