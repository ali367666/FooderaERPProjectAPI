using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Application.StoreMode.Approvals;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.StoreMode.Cash;

public record GetStoreRegistersQuery(int? RestaurantId) : IRequest<List<StoreCashRegisterResponse>>;

public record SaveStoreRegisterCommand(int? Id, SaveStoreCashRegisterRequest Request) : IRequest<StoreCashRegisterResponse>;

public record OpenStoreShiftCommand(OpenStoreShiftRequest Request) : IRequest<StoreShiftResponse>;

/// <summary>The current user's open shift, or null when they have none.</summary>
public record GetMyStoreShiftQuery : IRequest<StoreShiftResponse?>;

public record GetStoreShiftReportQuery(int ShiftId) : IRequest<StoreShiftReportResponse>;

public record CloseStoreShiftCommand(int ShiftId, CloseStoreShiftRequest Request) : IRequest<StoreShiftReportResponse>;

public record AddStoreCashMovementCommand(int ShiftId, StoreCashMovementRequest Request) : IRequest<StoreShiftReportResponse>;

public record GetStoreShiftsQuery(int? RegisterId, DateTime? From, DateTime? To) : IRequest<List<StoreShiftResponse>>;

internal static class StoreShiftMapper
{
    public static StoreShiftResponse ToResponse(StoreCashShift s, IReadOnlyDictionary<int, string> names) => new()
    {
        Id = s.Id,
        StoreCashRegisterId = s.StoreCashRegisterId,
        RegisterName = s.StoreCashRegister?.Name ?? string.Empty,
        RestaurantId = s.RestaurantId,
        CashierUserId = s.CashierUserId,
        CashierName = names.TryGetValue(s.CashierUserId, out var n) ? n : null,
        Status = s.Status,
        OpenedAtUtc = s.OpenedAtUtc,
        OpeningCash = s.OpeningCash,
        ClosedAtUtc = s.ClosedAtUtc,
        ExpectedCash = s.ExpectedCash,
        ActualCash = s.ActualCash,
        CashDifference = s.CashDifference,
    };

    /// <summary>Cash that stayed in the till from sales: cash handed over minus the change given back.</summary>
    public static decimal CashFromSales(IEnumerable<StoreSale> completedSales) =>
        completedSales.Sum(s => s.Payments.Where(p => p.Method == StorePaymentMethod.Cash).Sum(p => p.Amount) - s.ChangeAmount);
}

public class StoreCashHandlers :
    IRequestHandler<GetStoreRegistersQuery, List<StoreCashRegisterResponse>>,
    IRequestHandler<SaveStoreRegisterCommand, StoreCashRegisterResponse>,
    IRequestHandler<OpenStoreShiftCommand, StoreShiftResponse>,
    IRequestHandler<GetMyStoreShiftQuery, StoreShiftResponse?>,
    IRequestHandler<GetStoreShiftReportQuery, StoreShiftReportResponse>,
    IRequestHandler<CloseStoreShiftCommand, StoreShiftReportResponse>,
    IRequestHandler<AddStoreCashMovementCommand, StoreShiftReportResponse>,
    IRequestHandler<GetStoreShiftsQuery, List<StoreShiftResponse>>
{
    private readonly IStoreRepository _repository;
    private readonly IStoreCustomerRepository _customers;
    private readonly ICurrentUserService _currentUser;

    private readonly Application.StoreMode.Access.StoreAccessService _access;
    private readonly StoreApprovalService _approvals;

    public StoreCashHandlers(IStoreRepository repository, IStoreCustomerRepository customers, ICurrentUserService currentUser, Application.StoreMode.Access.StoreAccessService access, StoreApprovalService approvals)
    {
        _approvals = approvals;
        _access = access;
        _repository = repository;
        _customers = customers;
        _currentUser = currentUser;
    }

    // ---- Registers --------------------------------------------------------------------------

    public async Task<List<StoreCashRegisterResponse>> Handle(GetStoreRegistersQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var registers = await _repository.GetRegistersAsync(companyId, query.RestaurantId, ct);
        if (await _access.AllowedBranchesAsync(ct) is { } allowedBranches)
            registers = registers.Where(r => allowedBranches.Contains(r.RestaurantId)).ToList();
        var open = await _repository.GetOpenShiftsAsync(companyId, registers.Select(r => r.Id).ToList(), ct);
        var names = await _repository.GetUserNamesAsync(open.Select(s => s.CashierUserId), ct);

        return registers.Select(r =>
        {
            var shift = open.FirstOrDefault(s => s.StoreCashRegisterId == r.Id);
            return new StoreCashRegisterResponse
            {
                Id = r.Id,
                RestaurantId = r.RestaurantId,
                RestaurantName = r.Restaurant?.Name ?? string.Empty,
                WarehouseId = r.WarehouseId,
                WarehouseName = r.Warehouse?.Name ?? string.Empty,
                Name = r.Name,
                IsActive = r.IsActive,
                OpenShiftId = shift?.Id,
                OpenShiftCashierName = shift is not null && names.TryGetValue(shift.CashierUserId, out var n) ? n : null,
            };
        }).ToList();
    }

    public async Task<StoreCashRegisterResponse> Handle(SaveStoreRegisterCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BadRequestException("Kassanın adı boş ola bilməz.");
        if (!await _repository.BranchExistsAsync(companyId, dto.RestaurantId, ct))
            throw new BadRequestException("Filial tapılmadı.");
        if (!await _repository.WarehouseInBranchAsync(companyId, dto.WarehouseId, dto.RestaurantId, ct))
            throw new BadRequestException("Seçilmiş anbar bu filiala aid deyil.");

        StoreCashRegister register;
        if (command.Id is { } id)
        {
            register = await _repository.GetRegisterAsync(id, companyId, ct)
                ?? throw new NotFoundException("Kassa tapılmadı.");
            if (register.WarehouseId != dto.WarehouseId
                && await _repository.GetOpenShiftForRegisterAsync(register.Id, ct) is not null)
                throw new BadRequestException("Növbə açıq olan kassanın anbarını dəyişmək olmaz.");
        }
        else
        {
            register = new StoreCashRegister { CompanyId = companyId, CreatedByUserId = _currentUser.UserId };
            await _repository.AddRegisterAsync(register, ct);
        }

        register.RestaurantId = dto.RestaurantId;
        register.WarehouseId = dto.WarehouseId;
        register.Name = dto.Name.Trim();
        register.IsActive = dto.IsActive;
        await _repository.SaveChangesAsync(ct);

        var all = await _repository.GetRegistersAsync(companyId, dto.RestaurantId, ct);
        var saved = all.First(r => r.Id == register.Id);
        return new StoreCashRegisterResponse
        {
            Id = saved.Id,
            RestaurantId = saved.RestaurantId,
            RestaurantName = saved.Restaurant?.Name ?? string.Empty,
            WarehouseId = saved.WarehouseId,
            WarehouseName = saved.Warehouse?.Name ?? string.Empty,
            Name = saved.Name,
            IsActive = saved.IsActive,
        };
    }

    // ---- Shifts -----------------------------------------------------------------------------

    public async Task<StoreShiftResponse> Handle(OpenStoreShiftCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;
        var dto = command.Request;

        if (dto.OpeningCash < 0)
            throw new BadRequestException("İlkin nağd məbləğ mənfi ola bilməz.");

        var register = await _repository.GetRegisterAsync(dto.StoreCashRegisterId, companyId, ct)
            ?? throw new BadRequestException("Kassa tapılmadı.");
        if (!register.IsActive)
            throw new BadRequestException("Bu kassa aktiv deyil.");
        await _access.EnsureBranchAsync(register.RestaurantId, ct);
        if (await _repository.GetOpenShiftForRegisterAsync(register.Id, ct) is not null)
            throw new BadRequestException("Bu kassada artıq açıq növbə var.");
        if (await _repository.GetOpenShiftForUserAsync(companyId, userId, ct) is { } mine)
            throw new BadRequestException($"Sizin \"{mine.StoreCashRegister.Name}\" kassasında açıq növbəniz var. Əvvəlcə onu bağlayın.");

        var shift = new StoreCashShift
        {
            CompanyId = companyId,
            StoreCashRegisterId = register.Id,
            RestaurantId = register.RestaurantId,
            CashierUserId = userId,
            OpenedAtUtc = DateTime.UtcNow,
            OpeningCash = Math.Round(dto.OpeningCash, 2, MidpointRounding.AwayFromZero),
            CreatedByUserId = userId,
        };
        await _repository.AddShiftAsync(shift, ct);

        try
        {
            await _repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The unique index allows one open shift per till; this is the race of two openers.
            throw new BadRequestException("Bu kassada artıq açıq növbə var.");
        }

        shift.StoreCashRegister = register;
        var names = await _repository.GetUserNamesAsync(new[] { userId }, ct);
        return StoreShiftMapper.ToResponse(shift, names);
    }

    public async Task<StoreShiftResponse?> Handle(GetMyStoreShiftQuery query, CancellationToken ct)
    {
        var shift = await _repository.GetOpenShiftForUserAsync(_currentUser.CompanyId, _currentUser.UserId, ct);
        if (shift is null)
            return null;

        var names = await _repository.GetUserNamesAsync(new[] { shift.CashierUserId }, ct);
        return StoreShiftMapper.ToResponse(shift, names);
    }

    public async Task<List<StoreShiftResponse>> Handle(GetStoreShiftsQuery query, CancellationToken ct)
    {
        DateTime? fromUtc = query.From is { } f ? BusinessTime.ToUtc(f.Date + ReportPeriodResolver.DayStart) : null;
        DateTime? toUtc = query.To is { } t ? BusinessTime.ToUtc(t.Date.AddDays(1) + ReportPeriodResolver.DayStart) : null;

        var shifts = await _repository.SearchShiftsAsync(_currentUser.CompanyId, query.RegisterId, fromUtc, toUtc, ct);
        if (await _access.AllowedBranchesAsync(ct) is { } allowedBranches)
            shifts = shifts.Where(s => allowedBranches.Contains(s.RestaurantId)).ToList();
        var names = await _repository.GetUserNamesAsync(shifts.Select(s => s.CashierUserId), ct);
        return shifts.Select(s => StoreShiftMapper.ToResponse(s, names)).ToList();
    }

    public Task<StoreShiftReportResponse> Handle(GetStoreShiftReportQuery query, CancellationToken ct)
        => BuildReportAsync(query.ShiftId, ct);

    public async Task<StoreShiftReportResponse> Handle(AddStoreCashMovementCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;

        var shift = await _repository.GetShiftAsync(command.ShiftId, companyId, ct)
            ?? throw new NotFoundException("Növbə tapılmadı.");
        if (shift.Status != StoreShiftStatus.Open)
            throw new BadRequestException("Bağlanmış növbəyə mədaxil/məxaric yazmaq olmaz.");
        if (shift.CashierUserId != _currentUser.UserId)
            throw new BadRequestException("Bu növbə başqa kassirindir.");
        if (!Enum.IsDefined(dto.Type))
            throw new BadRequestException("Əməliyyat növü yanlışdır.");
        if (dto.Amount <= 0)
            throw new BadRequestException("Məbləğ 0-dan böyük olmalıdır.");
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new BadRequestException("Səbəb yazılmalıdır.");

        var report = await BuildReportAsync(shift.Id, ct);
        var amount = Math.Round(dto.Amount, 2, MidpointRounding.AwayFromZero);
        if (dto.Type == StoreCashMovementType.Out && amount > report.ExpectedCash)
            throw new BadRequestException($"Kassada bu qədər nağd yoxdur (var: {report.ExpectedCash:0.00}).");

        // Cash in and cash out are separate permissions; without the one for this kind a manager's credentials are needed.
        // (The older umbrella permission "StoreCash.Movement" still covers both.)
        var specific = dto.Type == StoreCashMovementType.In
            ? Domain.Constants.AppPermissions.StoreCashMovementIn
            : Domain.Constants.AppPermissions.StoreCashMovementOut;
        int? approver = null;
        if (!_currentUser.HasPermission(Domain.Constants.AppPermissions.StoreCashMovement))
            approver = await _approvals.RequireAsync(specific, dto.Approval, dto.Type == StoreCashMovementType.In ? "kassaya pul daxil etmək" : "kassadan pul çıxarmaq", ct);

        var movement = new StoreCashMovement
        {
            CompanyId = companyId,
            StoreCashShiftId = shift.Id,
            Type = dto.Type,
            Amount = amount,
            Reason = dto.Reason.Trim(),
            ApprovedByUserId = approver,
            CreatedByUserId = _currentUser.UserId,
        };
        await _repository.AddCashMovementAsync(movement, ct);
        await _repository.SaveChangesAsync(ct);

        if (approver is { } managerId)
        {
            await _approvals.LogAsync(dto.Type == StoreCashMovementType.In ? "CashIn" : "CashOut", "CashMovement", movement.Id, null, managerId, dto.Reason, ct);
            await _repository.SaveChangesAsync(ct);
        }

        return await BuildReportAsync(shift.Id, ct);
    }

    public async Task<StoreShiftReportResponse> Handle(CloseStoreShiftCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;

        var shift = await _repository.GetShiftAsync(command.ShiftId, companyId, ct)
            ?? throw new NotFoundException("Növbə tapılmadı.");
        if (shift.Status != StoreShiftStatus.Open)
            throw new BadRequestException("Növbə artıq bağlanıb.");
        if (shift.CashierUserId != _currentUser.UserId && !_currentUser.HasPermission(Domain.Constants.AppPermissions.StoreReportView))
            throw new BadRequestException("Başqa kassirin növbəsini yalnız hesabat icazəsi olan istifadəçi bağlaya bilər.");
        if (dto.ActualCash < 0)
            throw new BadRequestException("Faktiki nağd məbləğ mənfi ola bilməz.");

        var expected = (await BuildReportAsync(shift.Id, ct)).ExpectedCash;
        var actual = Math.Round(dto.ActualCash, 2, MidpointRounding.AwayFromZero);

        shift.Status = StoreShiftStatus.Closed;
        shift.ClosedAtUtc = DateTime.UtcNow;
        shift.ClosedByUserId = _currentUser.UserId;
        shift.ExpectedCash = expected;
        shift.ActualCash = actual;
        shift.CashDifference = actual - expected;
        shift.CloseNote = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
        await _repository.SaveChangesAsync(ct);

        return await BuildReportAsync(shift.Id, ct);
    }

    /// <summary>The X-Report of an open shift or the Z-Report of a closed one, worked out from its sales and cash movements.</summary>
    private async Task<StoreShiftReportResponse> BuildReportAsync(int shiftId, CancellationToken ct)
    {
        var shift = await _repository.GetShiftAsync(shiftId, _currentUser.CompanyId, ct)
            ?? throw new NotFoundException("Növbə tapılmadı.");

        var sales = await _repository.GetSalesForShiftAsync(shift.Id, ct);
        var movements = await _repository.GetCashMovementsAsync(shift.Id, ct);
        var returns = await _repository.GetReturnsForShiftAsync(shift.Id, ct);
        var customerPayments = await _customers.GetPaymentsForShiftAsync(shift.Id, ct);

        var completed = sales.Where(s => s.Status == StoreSaleStatus.Completed).ToList();
        var cancelled = sales.Where(s => s.Status == StoreSaleStatus.Cancelled).ToList();

        var payments = completed.SelectMany(s => s.Payments)
            .GroupBy(p => p.Method)
            .Select(g => new StorePaymentTotalResponse { Method = g.Key, Amount = g.Sum(p => p.Amount) })
            .ToList();
        // What stayed in the till as cash is what was handed over minus the change.
        var cashFromSales = StoreShiftMapper.CashFromSales(completed);
        var cashRow = payments.FirstOrDefault(p => p.Method == StorePaymentMethod.Cash);
        if (cashRow is not null)
            cashRow.Amount = cashFromSales;

        var cashIn = movements.Where(m => m.Type == StoreCashMovementType.In).Sum(m => m.Amount);
        var cashOut = movements.Where(m => m.Type == StoreCashMovementType.Out).Sum(m => m.Amount);
        var cashRefunds = returns.Where(r => r.RefundMethod == StorePaymentMethod.Cash).Sum(r => r.RefundAmount);
        var cardRefunds = returns.Where(r => r.RefundMethod == StorePaymentMethod.Card).Sum(r => r.RefundAmount);
        var customerCash = customerPayments.Where(p => p.Method == StorePaymentMethod.Cash).Sum(p => p.Amount);
        var customerCard = customerPayments.Where(p => p.Method == StorePaymentMethod.Card).Sum(p => p.Amount);

        var userIds = new List<int> { shift.CashierUserId };
        userIds.AddRange(movements.Select(m => m.CreatedByUserId).OfType<int>());
        var names = await _repository.GetUserNamesAsync(userIds, ct);

        return new StoreShiftReportResponse
        {
            Shift = StoreShiftMapper.ToResponse(shift, names),
            IsFinal = shift.Status == StoreShiftStatus.Closed,
            SalesCount = completed.Count,
            GrossSales = completed.Sum(s => s.SubTotal),
            Discounts = completed.Sum(s => s.DiscountTotal),
            NetSales = completed.Sum(s => s.Total),
            Vat = completed.Sum(s => s.VatTotal),
            CancelledCount = cancelled.Count,
            CancelledTotal = cancelled.Sum(s => s.Total),
            Payments = payments.OrderBy(p => p.Method).ToList(),
            CashIn = cashIn,
            CashOut = cashOut,
            CashMovements = movements.Select(m => new StoreCashMovementResponse
            {
                Id = m.Id,
                CreatedAtUtc = m.CreatedAtUtc,
                Type = m.Type,
                Amount = m.Amount,
                Reason = m.Reason,
                CreatedByUserName = m.CreatedByUserId is { } id && names.TryGetValue(id, out var n) ? n : null,
            }).ToList(),
            CustomerPaymentsCash = customerCash,
            CustomerPaymentsCard = customerCard,
            ReturnsCount = returns.Count,
            ReturnsTotal = returns.Sum(r => r.TotalAmount),
            CashRefunds = cashRefunds,
            CardRefunds = cardRefunds,
            ExpectedCash = shift.OpeningCash + cashFromSales + customerCash + cashIn - cashOut - cashRefunds,
        };
    }
}
