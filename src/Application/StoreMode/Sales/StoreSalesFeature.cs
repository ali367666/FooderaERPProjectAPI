using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Application.StoreMode.Approvals;
using Application.StoreMode.Notifications;
using Domain.Constants;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.StoreMode.Sales;

public record CreateStoreSaleCommand(CreateStoreSaleRequest Request) : IRequest<StoreSaleResponse>;

public record CancelStoreSaleCommand(int Id, CancelStoreSaleRequest Request) : IRequest<StoreSaleResponse>;

public record GetStoreSaleQuery(int Id) : IRequest<StoreSaleResponse>;

public record SearchStoreSalesQuery(
    DateTime? From, DateTime? To, int? ShiftId, StoreSaleStatus? Status, string? DocumentNo, int Page, int PageSize)
    : IRequest<StoreSalePageResponse>;

public class StoreSalesHandlers :
    IRequestHandler<CreateStoreSaleCommand, StoreSaleResponse>,
    IRequestHandler<CancelStoreSaleCommand, StoreSaleResponse>,
    IRequestHandler<GetStoreSaleQuery, StoreSaleResponse>,
    IRequestHandler<SearchStoreSalesQuery, StoreSalePageResponse>
{
    private readonly IStoreRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StoreSaleProcessor _processor;
    private readonly StoreApprovalService _approvals;
    private readonly IStoreCustomerRepository _customers;
    private readonly IStoreTradeRepository _trade;
    private readonly StoreNotifier _notifier;

    private readonly Application.StoreMode.Access.StoreAccessService _access;
    private readonly ICompanySettingsRepository _companySettings;

    public StoreSalesHandlers(
        Application.StoreMode.Access.StoreAccessService access,
        IStoreRepository repository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        StoreSaleProcessor processor,
        StoreApprovalService approvals,
        IStoreCustomerRepository customers,
        IStoreTradeRepository trade,
        StoreNotifier notifier,
        ICompanySettingsRepository companySettings)
    {
        _companySettings = companySettings;
        _access = access;
        _notifier = notifier;
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _processor = processor;
        _approvals = approvals;
        _customers = customers;
        _trade = trade;
    }

    // ---- Create -----------------------------------------------------------------------------

    public async Task<StoreSaleResponse> Handle(CreateStoreSaleCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;

        // A resent request returns the sale that was already made instead of making a second one.
        if (dto.OperationId != Guid.Empty
            && await _repository.GetSaleByOperationAsync(companyId, dto.OperationId, ct) is { } existing)
            return await _processor.MapAsync(existing, ct);

        if (dto.Payments is null || dto.Payments.Count == 0)
            throw new BadRequestException("Ödəniş daxil edilməyib.");

        var prepared = await _processor.PrepareAsync(dto, 0m, null, ct);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _processor.PersistAsync(prepared, ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            throw new BadRequestException("Stok başqa satışla dəyişdi. Yenidən cəhd edin.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);

            // Two identical requests at once: the second hits the unique operation id and gets the first sale.
            if (await _repository.GetSaleByOperationAsync(companyId, dto.OperationId, ct) is { } twin)
                return await _processor.MapAsync(twin, ct);
            throw;
        }

        // Products pushed to or below their minimum are announced to those who restock.
        foreach (var (name, quantity, minimum) in prepared.LowStock)
            await _notifier.NotifyHoldersAsync(
                AppPermissions.StoreStockView, "Minimum stok",
                $"\"{name}\" minimuma çatdı: qalıq {quantity:0.##}, minimum {minimum:0.##}.", "StoreLowStock", prepared.Sale.Id, "StoreSale", ct);

        return await _processor.MapAsync(prepared.Sale, ct);
    }

    // ---- Cancel -----------------------------------------------------------------------------

    public async Task<StoreSaleResponse> Handle(CancelStoreSaleCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;
        var reason = command.Request.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason))
            throw new BadRequestException("Ləğv səbəbi yazılmalıdır.");

        // The platform SuperAdmin can close changing sales for a company altogether.
        if ((await _companySettings.GetByCompanyIdAsync(companyId, ct))?.SalesEditLocked == true)
            throw new BadRequestException("Bu şirkətdə satışların dəyişdirilməsi (ləğvi) bağlıdır. Yalnız SuperAdmin açıb bağlaya bilər.");

        var sale = await _repository.GetSaleAsync(command.Id, companyId, ct)
            ?? throw new NotFoundException("Satış tapılmadı.");
        if (sale.Status != StoreSaleStatus.Completed)
            throw new BadRequestException("Bu satış artıq ləğv edilib.");
        if ((await _repository.GetReturnsForSaleAsync(sale.Id, ct)).Count > 0)
            throw new BadRequestException("Bu satışdan geri qaytarma olub; ləğv etmək olmaz.");

        var shift = await _repository.GetShiftAsync(sale.StoreCashShiftId, companyId, ct)
            ?? throw new NotFoundException("Satışın növbəsi tapılmadı.");
        if (shift.Status != StoreShiftStatus.Open)
            throw new BadRequestException("Növbə bağlanıb; satışı ləğv etmək olmur, geri qaytarma ilə düzəldin.");

        // A cashier without the cancel permission needs a manager.
        var approverId = await _approvals.RequireAsync(AppPermissions.StoreSaleCancel, command.Request.Approval, "satışı ləğv etmək", ct);

        foreach (var group in sale.Lines.GroupBy(l => l.StoreProductId))
        {
            var balance = await _repository.GetOrCreateBalanceAsync(companyId, sale.WarehouseId, group.Key, ct);
            var quantity = group.Sum(l => l.Quantity);
            var before = balance.Quantity;
            balance.Quantity += quantity;

            await _repository.AddMovementAsync(new StoreStockMovement
            {
                CompanyId = companyId,
                WarehouseId = sale.WarehouseId,
                StoreProductId = group.Key,
                Type = StoreStockMovementType.SaleCancel,
                Quantity = quantity,
                BalanceBefore = before,
                BalanceAfter = balance.Quantity,
                UnitCost = group.First().UnitCost,
                DocumentType = "Sale",
                DocumentId = sale.Id,
                DocumentNo = sale.DocumentNo,
                Reason = reason,
                CreatedByUserId = userId,
            }, ct);
        }

        // Serial / IMEI units go back on the shelf.
        var units = await _repository.GetSerialUnitsForSaleLinesAsync(sale.Lines.Select(l => l.Id).ToList(), ct);
        foreach (var unit in units)
        {
            unit.Status = StoreSerialStatus.InStock;
            unit.WarehouseId = sale.WarehouseId;
            unit.StoreSaleLineId = null;
            unit.SoldAtUtc = null;
            await _repository.AddSerialEventAsync(new StoreSerialEvent
            {
                StoreSerialUnitId = unit.Id,
                Type = StoreSerialEventType.Returned,
                WarehouseId = sale.WarehouseId,
                DocumentType = "Sale",
                DocumentId = sale.Id,
                DocumentNo = sale.DocumentNo,
                Note = $"Satış ləğvi: {reason}",
                CreatedByUserId = userId,
            }, ct);
        }

        // What the sale did to the customer's account is undone: debt, bonus spent and bonus earned.
        if (sale.StoreCustomerId is { } customerId)
        {
            if (sale.DebtAmount > 0)
                await _trade.AddLedgerEntryAsync(new StoreLedgerEntry
                {
                    CompanyId = companyId,
                    PartyType = StoreLedgerPartyType.Customer,
                    PartyId = customerId,
                    Type = StoreLedgerEntryType.SaleReturnCredit,
                    Amount = -sale.DebtAmount,
                    DocumentType = "Sale",
                    DocumentId = sale.Id,
                    DocumentNo = sale.DocumentNo,
                    Note = $"Satış ləğvi: {reason}",
                    CreatedByUserId = userId,
                }, ct);
            if (sale.BonusSpent > 0)
                await _customers.AddBonusEntryAsync(new StoreBonusEntry
                {
                    CompanyId = companyId,
                    StoreCustomerId = customerId,
                    Type = StoreBonusEntryType.Refund,
                    Amount = sale.BonusSpent,
                    DocumentType = "Sale",
                    DocumentId = sale.Id,
                    DocumentNo = sale.DocumentNo,
                    Note = $"Satış ləğvi: {reason}",
                    CreatedByUserId = userId,
                }, ct);
            if (sale.BonusEarned > 0)
                await _customers.AddBonusEntryAsync(new StoreBonusEntry
                {
                    CompanyId = companyId,
                    StoreCustomerId = customerId,
                    Type = StoreBonusEntryType.Reversal,
                    Amount = -sale.BonusEarned,
                    DocumentType = "Sale",
                    DocumentId = sale.Id,
                    DocumentNo = sale.DocumentNo,
                    Note = $"Satış ləğvi: {reason}",
                    CreatedByUserId = userId,
                }, ct);
        }

        sale.Status = StoreSaleStatus.Cancelled;
        sale.CancelledAtUtc = DateTime.UtcNow;
        sale.CancelledByUserId = userId;
        sale.CancelReason = reason;

        if (approverId is { } approver)
            await _approvals.LogAsync("SaleCancel", "Sale", sale.Id, sale.DocumentNo, approver, command.Request.Approval?.Reason ?? reason, ct);

        try
        {
            await _repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BadRequestException("Satış və ya stok başqa əməliyyatla dəyişdi. Yenidən cəhd edin.");
        }

        return await _processor.MapAsync(sale, ct);
    }

    // ---- Read -------------------------------------------------------------------------------

    public async Task<StoreSaleResponse> Handle(GetStoreSaleQuery query, CancellationToken ct)
    {
        var sale = await _repository.GetSaleAsync(query.Id, _currentUser.CompanyId, ct)
            ?? throw new NotFoundException("Satış tapılmadı.");
        await _access.EnsureBranchAsync(sale.RestaurantId, ct);
        return await _processor.MapAsync(sale, ct);
    }

    public async Task<StoreSalePageResponse> Handle(SearchStoreSalesQuery query, CancellationToken ct)
    {
        DateTime? fromUtc = query.From is { } f ? BusinessTime.ToUtc(f.Date + ReportPeriodResolver.DayStart) : null;
        DateTime? toUtc = query.To is { } t ? BusinessTime.ToUtc(t.Date.AddDays(1) + ReportPeriodResolver.DayStart) : null;
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var page = Math.Max(1, query.Page);

        var (items, total) = await _repository.SearchSalesAsync(
            _currentUser.CompanyId, fromUtc, toUtc, query.ShiftId, query.Status, query.DocumentNo, (page - 1) * pageSize, pageSize,
            await _access.AllowedBranchesAsync(ct), ct);
        var names = await _repository.GetUserNamesAsync(items.Select(s => s.CreatedByUserId).OfType<int>(), ct);

        return new StoreSalePageResponse
        {
            Total = total,
            Items = items.Select(s => new StoreSaleListItemResponse
            {
                Id = s.Id,
                DocumentNo = s.DocumentNo,
                Status = s.Status,
                CreatedAtUtc = s.CreatedAtUtc,
                CashierName = s.CreatedByUserId is { } id && names.TryGetValue(id, out var n) ? n : null,
                RegisterName = s.StoreCashRegister?.Name,
                Total = s.Total,
                LineCount = s.Lines.Count,
            }).ToList(),
        };
    }
}
