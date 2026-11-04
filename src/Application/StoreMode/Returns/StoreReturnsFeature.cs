using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Application.StoreMode.Approvals;
using Application.StoreMode.Sales;
using Domain.Constants;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.StoreMode.Returns;

public record FindStoreSaleForReturnQuery(string DocumentNo) : IRequest<StoreSaleForReturnResponse>;

public record CreateStoreReturnCommand(CreateStoreReturnRequest Request) : IRequest<StoreReturnResponse>;

public record CreateStoreExchangeCommand(CreateStoreExchangeRequest Request) : IRequest<StoreExchangeResponse>;

public record GetStoreReturnQuery(int Id) : IRequest<StoreReturnResponse>;

public record SearchStoreReturnsQuery(DateTime? From, DateTime? To) : IRequest<List<StoreReturnResponse>>;

/// <summary>A return that passed every check, ready to be saved by <see cref="StoreReturnProcessor.PersistAsync"/>.</summary>
public sealed class PreparedReturn
{
    public StoreSaleReturn Return { get; init; } = default!;
    public int WarehouseId { get; init; }
    public decimal TotalAmount => Return.TotalAmount;
    public List<(StoreSaleReturnLine Line, List<StoreSerialUnit> Units)> Lines { get; init; } = new();
    public List<(string Action, int ApproverId, string? Reason)> Approvals { get; init; } = new();

    /// <summary>Sellable quantity per product that goes back on the shelf - an exchange may sell it again at once.</summary>
    public Dictionary<int, decimal> IncomingStock { get; init; } = new();
}

public sealed class StoreReturnProcessor
{
    private readonly IStoreRepository _repository;
    private readonly IStoreCustomerRepository _customers;
    private readonly IStoreTradeRepository _trade;
    private readonly ICurrentUserService _currentUser;
    private readonly StoreApprovalService _approvals;
    private readonly Application.StoreMode.Access.StoreAccessService _access;

    public StoreReturnProcessor(
        IStoreRepository repository,
        IStoreCustomerRepository customers,
        IStoreTradeRepository trade,
        ICurrentUserService currentUser,
        StoreApprovalService approvals,
        Application.StoreMode.Access.StoreAccessService access)
    {
        _access = access;
        _repository = repository;
        _customers = customers;
        _trade = trade;
        _currentUser = currentUser;
        _approvals = approvals;
    }

    private static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    public async Task<PreparedReturn> PrepareAsync(CreateStoreReturnRequest dto, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;

        if (dto.OperationId == Guid.Empty)
            throw new BadRequestException("Əməliyyat identifikatoru (operationId) göndərilməlidir.");
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new BadRequestException("Qaytarma səbəbi yazılmalıdır.");
        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new BadRequestException("Qaytarılacaq məhsul seçilməyib.");
        if (dto.Lines.GroupBy(l => l.SaleLineId).Any(g => g.Count() > 1))
            throw new BadRequestException("Eyni satış sətri iki dəfə seçilib.");
        if (dto.RefundMethod is not (StorePaymentMethod.Cash or StorePaymentMethod.Card))
            throw new BadRequestException("Pul yalnız nağd və ya karta qaytarıla bilər.");

        var sale = await _repository.GetSaleAsync(dto.StoreSaleId, companyId, ct)
            ?? throw new BadRequestException("İlkin satış tapılmadı.");
        await _access.EnsureBranchAsync(sale.RestaurantId, ct);
        if (sale.Status != StoreSaleStatus.Completed)
            throw new BadRequestException("Ləğv edilmiş satışdan qaytarma olmaz.");

        // The goods come back at the till the cashier is working at.
        var shift = await _repository.GetOpenShiftForUserAsync(companyId, userId, ct)
            ?? throw new BadRequestException("Qaytarma üçün əvvəlcə növbəni açın.");
        var register = shift.StoreCashRegister;

        var approverId = await _approvals.RequireAsync(AppPermissions.StoreSaleReturn, dto.Approval, "geri qaytarma", ct);

        var previous = await _repository.GetReturnsForSaleAsync(sale.Id, ct);
        var returnedQty = previous.SelectMany(r => r.Lines).GroupBy(l => l.StoreSaleLineId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
        var returnedAmount = previous.SelectMany(r => r.Lines).GroupBy(l => l.StoreSaleLineId).ToDictionary(g => g.Key, g => g.Sum(l => l.Amount));

        var productIds = sale.Lines.Select(l => l.StoreProductId).Distinct().ToList();
        var products = (await _repository.GetProductsByIdsAsync(companyId, productIds, ct)).ToDictionary(p => p.Id);

        var wantedSerials = dto.Lines.SelectMany(l => l.SerialNumbers ?? new()).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        var serialUnits = wantedSerials.Count == 0
            ? new List<StoreSerialUnit>()
            : await _repository.FindSerialUnitsAsync(companyId, wantedSerials, ct);

        var lines = new List<(StoreSaleReturnLine, List<StoreSerialUnit>)>();
        var incoming = new Dictionary<int, decimal>();

        foreach (var req in dto.Lines)
        {
            var saleLine = sale.Lines.FirstOrDefault(l => l.Id == req.SaleLineId)
                ?? throw new BadRequestException("Seçilmiş sətir bu satışa aid deyil.");
            if (!Enum.IsDefined(req.Condition))
                throw new BadRequestException("Məhsulun vəziyyəti yanlışdır.");
            if (req.Quantity <= 0)
                throw new BadRequestException($"\"{saleLine.ProductName}\" üçün miqdar 0-dan böyük olmalıdır.");

            var already = returnedQty.GetValueOrDefault(saleLine.Id);
            var remaining = saleLine.Quantity - already;
            if (req.Quantity > remaining)
                throw new BadRequestException(
                    $"\"{saleLine.ProductName}\" üçün ən çox {remaining} qaytarmaq olar (satılıb: {saleLine.Quantity}, əvvəl qaytarılıb: {already}).");

            var unitAmount = Math.Round(saleLine.LineTotal / saleLine.Quantity, 4, MidpointRounding.AwayFromZero);
            // Returning the rest of a line pays back exactly what is left of it, so cents never drift.
            var amount = req.Quantity == remaining
                ? saleLine.LineTotal - returnedAmount.GetValueOrDefault(saleLine.Id)
                : R2(req.Quantity * unitAmount);

            var units = new List<StoreSerialUnit>();
            var tracks = products.TryGetValue(saleLine.StoreProductId, out var product) && product.TracksSerial;
            var serials = (req.SerialNumbers ?? new()).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            if (tracks)
            {
                if (serials.Count != (int)req.Quantity || req.Quantity != Math.Floor(req.Quantity))
                    throw new BadRequestException($"\"{saleLine.ProductName}\" üçün {(int)req.Quantity} ədəd IMEI/serial seçilməlidir.");
                foreach (var serial in serials)
                {
                    var unit = serialUnits.FirstOrDefault(u => u.SerialNumber == serial);
                    if (unit is null || unit.StoreSaleLineId != saleLine.Id || unit.Status != StoreSerialStatus.Sold)
                        throw new BadRequestException($"\"{serial}\" nömrəsi bu satış sətrində satılmayıb və ya artıq qaytarılıb.");
                    units.Add(unit);
                }
            }
            else if (serials.Count > 0)
            {
                throw new BadRequestException($"\"{saleLine.ProductName}\" IMEI/serial ilə izlənmir.");
            }

            lines.Add((new StoreSaleReturnLine
            {
                StoreSaleLineId = saleLine.Id,
                StoreProductId = saleLine.StoreProductId,
                ProductName = saleLine.ProductName,
                Quantity = req.Quantity,
                UnitAmount = unitAmount,
                Amount = amount,
                UnitCost = saleLine.UnitCost,
                Condition = req.Condition,
            }, units));

            if (req.Condition == StoreReturnCondition.Sellable)
                incoming[saleLine.StoreProductId] = incoming.GetValueOrDefault(saleLine.StoreProductId) + req.Quantity;
        }

        var total = lines.Sum(l => l.Item1.Amount);

        // The sale's customer effects are returned in proportion to what is returned: a share of the debt is
        // cancelled, of the bonus paid is given back, and of the bonus earned is taken back.
        decimal debtCredit = 0, bonusRefund = 0, bonusClawback = 0;
        if (sale.StoreCustomerId is not null && sale.Total > 0)
        {
            var ratio = total / sale.Total;
            var finalReturn = lines.All(l => l.Item1.Quantity == sale.Lines.First(x => x.Id == l.Item1.StoreSaleLineId).Quantity - returnedQty.GetValueOrDefault(l.Item1.StoreSaleLineId))
                && sale.Lines.All(sl => lines.Any(l => l.Item1.StoreSaleLineId == sl.Id) || returnedQty.GetValueOrDefault(sl.Id) >= sl.Quantity);

            decimal Share(decimal original, decimal alreadyGiven)
                => Math.Min(Math.Max(0, original - alreadyGiven), finalReturn ? original - alreadyGiven : R2(original * ratio));

            debtCredit = Share(sale.DebtAmount, previous.Sum(r => r.DebtCreditAmount));
            bonusRefund = Share(sale.BonusSpent, previous.Sum(r => r.BonusRefundAmount));
            bonusClawback = Share(sale.BonusEarned, previous.Sum(r => r.BonusClawbackAmount));
        }

        var approvals = new List<(string, int, string?)>();
        if (approverId is { } approver)
            approvals.Add(("SaleReturn", approver, dto.Approval?.Reason ?? dto.Reason));

        var entity = new StoreSaleReturn
        {
            CompanyId = companyId,
            DocumentNo = Guid.NewGuid().ToString("N"),
            OperationId = dto.OperationId,
            StoreSaleId = sale.Id,
            StoreSale = sale,
            RestaurantId = register.RestaurantId,
            WarehouseId = register.WarehouseId,
            StoreCashRegisterId = register.Id,
            StoreCashRegister = register,
            StoreCashShiftId = shift.Id,
            Reason = dto.Reason.Trim(),
            TotalAmount = total,
            // What is paid out: the value less the debt it cancels and the bonus given back instead of money.
            RefundAmount = Math.Max(0, total - debtCredit - bonusRefund),
            DebtCreditAmount = debtCredit,
            BonusRefundAmount = bonusRefund,
            BonusClawbackAmount = bonusClawback,
            RefundMethod = dto.RefundMethod,
            CreatedByUserId = userId,
            Lines = lines.Select(l => l.Item1).ToList(),
        };

        return new PreparedReturn
        {
            Return = entity,
            WarehouseId = register.WarehouseId,
            Lines = lines,
            Approvals = approvals,
            IncomingStock = incoming,
        };
    }

    /// <summary>Saves the return, puts sellable goods back into stock and settles the serial units. Run inside the caller's transaction.</summary>
    public async Task PersistAsync(PreparedReturn prepared, CancellationToken ct)
    {
        var ret = prepared.Return;
        var userId = _currentUser.UserId;

        await _repository.AddReturnAsync(ret, ct);
        await _repository.SaveChangesAsync(ct);
        ret.DocumentNo = $"R-{ret.Id:D7}";

        foreach (var (line, units) in prepared.Lines)
        {
            if (line.Condition != StoreReturnCondition.Inspect)
            {
                // Goods back on the shelf; waste goods go straight out again, so the ledger shows both steps.
                var balance = await _repository.GetOrCreateBalanceAsync(ret.CompanyId, prepared.WarehouseId, line.StoreProductId, ct);
                var before = balance.Quantity;
                balance.Quantity += line.Quantity;
                await _repository.AddMovementAsync(Movement(ret, prepared.WarehouseId, line, StoreStockMovementType.SaleReturn, line.Quantity, before, balance.Quantity, null), ct);

                if (line.Condition == StoreReturnCondition.Waste)
                {
                    var afterReturn = balance.Quantity;
                    balance.Quantity -= line.Quantity;
                    await _repository.AddMovementAsync(Movement(ret, prepared.WarehouseId, line, StoreStockMovementType.Waste, -line.Quantity, afterReturn, balance.Quantity, "Qaytarılan mal istifadəyə yararsızdır"), ct);
                }
            }

            foreach (var unit in units)
            {
                var (status, type, note) = line.Condition switch
                {
                    StoreReturnCondition.Sellable => (StoreSerialStatus.InStock, StoreSerialEventType.Returned, "Qaytarma: satışa yararlı"),
                    StoreReturnCondition.Waste => (StoreSerialStatus.WrittenOff, StoreSerialEventType.WrittenOff, "Qaytarma: yararsız"),
                    _ => (StoreSerialStatus.Inspection, StoreSerialEventType.Service, "Qaytarma: yoxlanılmalıdır"),
                };
                unit.Status = status;
                unit.WarehouseId = status == StoreSerialStatus.InStock ? prepared.WarehouseId : null;
                unit.StoreSaleLineId = null;
                unit.SoldAtUtc = null;
                await _repository.AddSerialEventAsync(new StoreSerialEvent
                {
                    StoreSerialUnitId = unit.Id,
                    Type = type,
                    WarehouseId = prepared.WarehouseId,
                    DocumentType = "SaleReturn",
                    DocumentId = ret.Id,
                    DocumentNo = ret.DocumentNo,
                    Note = note,
                    CreatedByUserId = userId,
                }, ct);
            }
        }

        if (ret.StoreSale.StoreCustomerId is { } customerId)
        {
            if (ret.DebtCreditAmount > 0)
                await _trade.AddLedgerEntryAsync(new StoreLedgerEntry
                {
                    CompanyId = ret.CompanyId,
                    PartyType = StoreLedgerPartyType.Customer,
                    PartyId = customerId,
                    Type = StoreLedgerEntryType.SaleReturnCredit,
                    Amount = -ret.DebtCreditAmount,
                    DocumentType = "SaleReturn",
                    DocumentId = ret.Id,
                    DocumentNo = ret.DocumentNo,
                    Note = ret.Reason,
                    CreatedByUserId = userId,
                }, ct);
            if (ret.BonusRefundAmount > 0)
                await _customers.AddBonusEntryAsync(new StoreBonusEntry
                {
                    CompanyId = ret.CompanyId,
                    StoreCustomerId = customerId,
                    Type = StoreBonusEntryType.Refund,
                    Amount = ret.BonusRefundAmount,
                    DocumentType = "SaleReturn",
                    DocumentId = ret.Id,
                    DocumentNo = ret.DocumentNo,
                    Note = ret.Reason,
                    CreatedByUserId = userId,
                }, ct);
            if (ret.BonusClawbackAmount > 0)
                await _customers.AddBonusEntryAsync(new StoreBonusEntry
                {
                    CompanyId = ret.CompanyId,
                    StoreCustomerId = customerId,
                    Type = StoreBonusEntryType.Reversal,
                    Amount = -ret.BonusClawbackAmount,
                    DocumentType = "SaleReturn",
                    DocumentId = ret.Id,
                    DocumentNo = ret.DocumentNo,
                    Note = ret.Reason,
                    CreatedByUserId = userId,
                }, ct);
        }

        foreach (var (action, approverId, reason) in prepared.Approvals)
            await _approvals.LogAsync(action, "SaleReturn", ret.Id, ret.DocumentNo, approverId, reason, ct);

        await _repository.SaveChangesAsync(ct);
    }

    private StoreStockMovement Movement(
        StoreSaleReturn ret, int warehouseId, StoreSaleReturnLine line, StoreStockMovementType type,
        decimal quantity, decimal before, decimal after, string? reason) => new()
    {
        CompanyId = ret.CompanyId,
        WarehouseId = warehouseId,
        StoreProductId = line.StoreProductId,
        Type = type,
        Quantity = quantity,
        BalanceBefore = before,
        BalanceAfter = after,
        UnitCost = line.UnitCost,
        DocumentType = "SaleReturn",
        DocumentId = ret.Id,
        DocumentNo = ret.DocumentNo,
        Reason = reason ?? ret.Reason,
        CreatedByUserId = _currentUser.UserId,
    };

    public async Task<StoreReturnResponse> MapAsync(StoreSaleReturn r, CancellationToken ct)
    {
        var names = await _repository.GetUserNamesAsync(new[] { r.CreatedByUserId }.OfType<int>(), ct);
        return new StoreReturnResponse
        {
            Id = r.Id,
            DocumentNo = r.DocumentNo,
            CreatedAtUtc = r.CreatedAtUtc,
            StoreSaleId = r.StoreSaleId,
            SaleDocumentNo = r.StoreSale?.DocumentNo ?? string.Empty,
            RegisterName = r.StoreCashRegister?.Name,
            CashierName = r.CreatedByUserId is { } id && names.TryGetValue(id, out var n) ? n : null,
            Reason = r.Reason,
            TotalAmount = r.TotalAmount,
            RefundAmount = r.RefundAmount,
            RefundMethod = r.RefundMethod,
            DebtCreditAmount = r.DebtCreditAmount,
            BonusRefundAmount = r.BonusRefundAmount,
            BonusClawbackAmount = r.BonusClawbackAmount,
            ExchangeSaleId = r.ExchangeSaleId,
            Lines = r.Lines.OrderBy(l => l.Id).Select(l => new StoreReturnLineResponse
            {
                ProductId = l.StoreProductId,
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                Amount = l.Amount,
                Condition = l.Condition,
            }).ToList(),
        };
    }
}

public class StoreReturnsHandlers :
    IRequestHandler<FindStoreSaleForReturnQuery, StoreSaleForReturnResponse>,
    IRequestHandler<CreateStoreReturnCommand, StoreReturnResponse>,
    IRequestHandler<CreateStoreExchangeCommand, StoreExchangeResponse>,
    IRequestHandler<GetStoreReturnQuery, StoreReturnResponse>,
    IRequestHandler<SearchStoreReturnsQuery, List<StoreReturnResponse>>
{
    private readonly IStoreRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StoreReturnProcessor _returns;
    private readonly StoreSaleProcessor _sales;

    private readonly Application.StoreMode.Access.StoreAccessService _access;

    public StoreReturnsHandlers(
        IStoreRepository repository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        StoreReturnProcessor returns,
        StoreSaleProcessor sales,
        Application.StoreMode.Access.StoreAccessService access)
    {
        _access = access;
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _returns = returns;
        _sales = sales;
    }

    public async Task<StoreSaleForReturnResponse> Handle(FindStoreSaleForReturnQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var sale = await _repository.GetSaleByDocumentNoAsync(companyId, (query.DocumentNo ?? string.Empty).Trim(), ct)
            ?? throw new NotFoundException("Bu nömrəli çek tapılmadı.");
        await _access.EnsureBranchAsync(sale.RestaurantId, ct);
        if (sale.Status != StoreSaleStatus.Completed)
            throw new BadRequestException("Bu çek ləğv edilib; ondan qaytarma olmaz.");

        var previous = await _repository.GetReturnsForSaleAsync(sale.Id, ct);
        var returnedQty = previous.SelectMany(r => r.Lines).GroupBy(l => l.StoreSaleLineId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
        var products = (await _repository.GetProductsByIdsAsync(companyId, sale.Lines.Select(l => l.StoreProductId).Distinct().ToList(), ct))
            .ToDictionary(p => p.Id);
        var serials = (await _repository.GetSerialUnitsForSaleLinesAsync(sale.Lines.Select(l => l.Id).ToList(), ct))
            .Where(u => u.Status == StoreSerialStatus.Sold)
            .GroupBy(u => u.StoreSaleLineId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(u => u.SerialNumber).OrderBy(x => x).ToList());

        return new StoreSaleForReturnResponse
        {
            Sale = await _sales.MapAsync(sale, ct),
            Lines = sale.Lines.OrderBy(l => l.Id).Select(l =>
            {
                var returned = returnedQty.GetValueOrDefault(l.Id);
                return new StoreReturnableLineResponse
                {
                    SaleLineId = l.Id,
                    ProductId = l.StoreProductId,
                    ProductName = l.ProductName,
                    TracksSerial = products.TryGetValue(l.StoreProductId, out var p) && p.TracksSerial,
                    SoldQuantity = l.Quantity,
                    ReturnedQuantity = returned,
                    ReturnableQuantity = l.Quantity - returned,
                    UnitAmount = Math.Round(l.LineTotal / l.Quantity, 4, MidpointRounding.AwayFromZero),
                    SoldSerials = serials.GetValueOrDefault(l.Id) ?? new(),
                };
            }).ToList(),
        };
    }

    public async Task<StoreReturnResponse> Handle(CreateStoreReturnCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;

        if (dto.OperationId != Guid.Empty
            && await _repository.GetReturnByOperationAsync(companyId, dto.OperationId, ct) is { } existing)
            return await _returns.MapAsync(existing, ct);

        var prepared = await _returns.PrepareAsync(dto, ct);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _returns.PersistAsync(prepared, ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            throw new BadRequestException("Stok başqa əməliyyatla dəyişdi. Yenidən cəhd edin.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            if (await _repository.GetReturnByOperationAsync(companyId, dto.OperationId, ct) is { } twin)
                return await _returns.MapAsync(twin, ct);
            throw;
        }

        return await _returns.MapAsync(prepared.Return, ct);
    }

    public async Task<StoreExchangeResponse> Handle(CreateStoreExchangeCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;

        if (dto.Return is null || dto.NewSale is null)
            throw new BadRequestException("Dəyişdirmədə həm qaytarılan, həm yeni məhsullar olmalıdır.");

        if (dto.Return.OperationId != Guid.Empty
            && await _repository.GetReturnByOperationAsync(companyId, dto.Return.OperationId, ct) is { } existing
            && existing.ExchangeSaleId is { } existingSaleId
            && await _repository.GetSaleAsync(existingSaleId, companyId, ct) is { } existingSale)
            return new StoreExchangeResponse
            {
                Return = await _returns.MapAsync(existing, ct),
                Sale = await _sales.MapAsync(existingSale, ct),
            };

        var preparedReturn = await _returns.PrepareAsync(dto.Return, ct);
        if (dto.NewSale.StoreCashRegisterId != preparedReturn.Return.StoreCashRegisterId)
            throw new BadRequestException("Dəyişdirmə növbəsi açıq olan kassada aparılmalıdır.");

        // The returned value pays for the new goods first; what is left over goes back as a refund.
        var refundable = preparedReturn.Return.RefundAmount;
        var preparedSale = await _sales.PrepareAsync(dto.NewSale, refundable, preparedReturn.IncomingStock, ct);
        var creditUsed = preparedSale.Sale.Payments
            .Where(p => p.Method == StorePaymentMethod.ReturnCredit).Sum(p => p.Amount);
        preparedReturn.Return.RefundAmount = refundable - creditUsed;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _returns.PersistAsync(preparedReturn, ct);
            await _sales.PersistAsync(preparedSale, ct);
            preparedReturn.Return.ExchangeSaleId = preparedSale.Sale.Id;
            await _repository.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            throw new BadRequestException("Stok başqa əməliyyatla dəyişdi. Yenidən cəhd edin.");
        }

        return new StoreExchangeResponse
        {
            Return = await _returns.MapAsync(preparedReturn.Return, ct),
            Sale = await _sales.MapAsync(preparedSale.Sale, ct),
        };
    }

    public async Task<StoreReturnResponse> Handle(GetStoreReturnQuery query, CancellationToken ct)
    {
        var ret = await _repository.GetReturnAsync(query.Id, _currentUser.CompanyId, ct)
            ?? throw new NotFoundException("Qaytarma tapılmadı.");
        return await _returns.MapAsync(ret, ct);
    }

    public async Task<List<StoreReturnResponse>> Handle(SearchStoreReturnsQuery query, CancellationToken ct)
    {
        DateTime? fromUtc = query.From is { } f ? BusinessTime.ToUtc(f.Date + ReportPeriodResolver.DayStart) : null;
        DateTime? toUtc = query.To is { } t ? BusinessTime.ToUtc(t.Date.AddDays(1) + ReportPeriodResolver.DayStart) : null;

        var rows = await _repository.SearchReturnsAsync(_currentUser.CompanyId, fromUtc, toUtc, 200, ct);
        var result = new List<StoreReturnResponse>();
        foreach (var r in rows)
            result.Add(await _returns.MapAsync(r, ct));
        return result;
    }
}
