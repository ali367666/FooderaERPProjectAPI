using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Cash;

// ---- DTOs ----------------------------------------------------------------------------------

public class StoreExpenseItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class SaveStoreExpenseItemRequest
{
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

public class CreateStoreExpenseLineRequest
{
    public int RegisterId { get; set; }
    public int ExpenseItemId { get; set; }

    /// <summary>To whom the money is paid (kimə).</summary>
    public string? Payee { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class CreateStoreExpenseRequest
{
    public string? Note { get; set; }
    public List<CreateStoreExpenseLineRequest> Lines { get; set; } = new();
}

public class StoreExpenseLineResponse
{
    public int Id { get; set; }
    public int RegisterId { get; set; }
    public string RegisterName { get; set; } = default!;
    public int ExpenseItemId { get; set; }
    public string ExpenseItemName { get; set; } = default!;
    public string? Payee { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class StoreExpenseResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public DateTime DocumentDate { get; set; }
    public string? Note { get; set; }
    public string? AuthorName { get; set; }
    public decimal Total { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancelReason { get; set; }
    public List<StoreExpenseLineResponse> Lines { get; set; } = new();
}

public class StoreExpenseListResponse
{
    /// <summary>The sum of the lines shown, cancelled documents left out.</summary>
    public decimal Total { get; set; }

    /// <summary>The same sum per expense item (cancelled documents left out).</summary>
    public List<StoreExpenseItemTotal> ByItem { get; set; } = new();

    public List<StoreExpenseResponse> Items { get; set; } = new();
}

public class StoreExpenseItemTotal
{
    public string Name { get; set; } = default!;
    public decimal Amount { get; set; }
}

// ---- Queries and commands ------------------------------------------------------------------

public record GetStoreExpenseItemsQuery(bool IncludeInactive) : IRequest<List<StoreExpenseItemResponse>>;

public record SaveStoreExpenseItemCommand(int? Id, SaveStoreExpenseItemRequest Request) : IRequest<StoreExpenseItemResponse>;

public record GetStoreExpensesQuery(
    DateTime? From, DateTime? To, int? RegisterId, int? ExpenseItemId, string? Payee, string? Code, bool IncludeCancelled)
    : IRequest<StoreExpenseListResponse>;

public record CreateStoreExpenseCommand(CreateStoreExpenseRequest Request) : IRequest<StoreExpenseResponse>;

public record CancelStoreExpenseCommand(int Id, string Reason) : IRequest<StoreExpenseResponse>;

public class StoreExpensesHandlers :
    IRequestHandler<GetStoreExpenseItemsQuery, List<StoreExpenseItemResponse>>,
    IRequestHandler<SaveStoreExpenseItemCommand, StoreExpenseItemResponse>,
    IRequestHandler<GetStoreExpensesQuery, StoreExpenseListResponse>,
    IRequestHandler<CreateStoreExpenseCommand, StoreExpenseResponse>,
    IRequestHandler<CancelStoreExpenseCommand, StoreExpenseResponse>
{
    private readonly IStoreExpenseRepository _expenses;
    private readonly IStoreRepository _store;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public StoreExpensesHandlers(
        IStoreExpenseRepository expenses, IStoreRepository store, ICurrentUserService currentUser, IUnitOfWork unitOfWork, IMediator mediator)
    {
        _expenses = expenses;
        _store = store;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    private static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private static StoreExpenseItemResponse Map(StoreExpenseItem i) => new() { Id = i.Id, Name = i.Name, IsActive = i.IsActive };

    // ---- Items ------------------------------------------------------------------------------

    public async Task<List<StoreExpenseItemResponse>> Handle(GetStoreExpenseItemsQuery query, CancellationToken ct)
        => (await _expenses.GetItemsAsync(_currentUser.CompanyId, query.IncludeInactive, ct)).Select(Map).ToList();

    public async Task<StoreExpenseItemResponse> Handle(SaveStoreExpenseItemCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var name = command.Request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            throw new BadRequestException("Xərc maddəsinin adı yazılmalıdır.");
        if (name.Length > 100)
            throw new BadRequestException("Xərc maddəsinin adı 100 simvoldan uzun ola bilməz.");
        if (await _expenses.ItemNameExistsAsync(companyId, name, command.Id, ct))
            throw new BadRequestException("Bu adda xərc maddəsi artıq var.");

        StoreExpenseItem item;
        if (command.Id is { } id)
        {
            item = await _expenses.GetItemAsync(id, companyId, ct) ?? throw new NotFoundException("Xərc maddəsi tapılmadı.");
            item.Name = name;
            item.IsActive = command.Request.IsActive;
        }
        else
        {
            item = new StoreExpenseItem { CompanyId = companyId, Name = name, IsActive = command.Request.IsActive, CreatedByUserId = _currentUser.UserId };
            await _expenses.AddItemAsync(item, ct);
        }

        await _expenses.SaveChangesAsync(ct);
        return Map(item);
    }

    // ---- List -------------------------------------------------------------------------------

    private async Task<StoreExpenseResponse> MapExpenseAsync(StoreExpense e, IReadOnlyDictionary<int, string> names)
    {
        await Task.CompletedTask;
        return new StoreExpenseResponse
        {
            Id = e.Id,
            DocumentNo = e.DocumentNo,
            DocumentDate = e.DocumentDate,
            Note = e.Note,
            AuthorName = e.CreatedByUserId is { } u && names.TryGetValue(u, out var n) ? n : null,
            Total = R2(e.Lines.Sum(l => l.Amount)),
            IsCancelled = e.CancelledAtUtc != null,
            CancelledAtUtc = e.CancelledAtUtc,
            CancelReason = e.CancelReason,
            Lines = e.Lines.OrderBy(l => l.Id).Select(l => new StoreExpenseLineResponse
            {
                Id = l.Id,
                RegisterId = l.StoreCashRegisterId,
                RegisterName = l.StoreCashRegister?.Name ?? string.Empty,
                ExpenseItemId = l.StoreExpenseItemId,
                ExpenseItemName = l.StoreExpenseItem?.Name ?? string.Empty,
                Payee = l.Payee,
                Amount = l.Amount,
                Note = l.Note,
            }).ToList(),
        };
    }

    public async Task<StoreExpenseListResponse> Handle(GetStoreExpensesQuery query, CancellationToken ct)
    {
        DateTime? fromUtc = query.From is { } f ? ReportPeriodResolver.StoreFromUtc(f) : null;
        DateTime? toUtc = query.To is { } t ? ReportPeriodResolver.StoreToUtc(t) : null;

        var docs = await _expenses.SearchAsync(_currentUser.CompanyId, fromUtc, toUtc, ct);

        if (!string.IsNullOrWhiteSpace(query.Code))
            docs = docs.Where(d => d.DocumentNo.Contains(query.Code.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (!query.IncludeCancelled)
            docs = docs.Where(d => d.CancelledAtUtc == null).ToList();

        // Line filters keep a document when any of its lines matches.
        var payee = query.Payee?.Trim();
        bool LineMatches(StoreExpenseLine l)
            => (query.RegisterId is null || l.StoreCashRegisterId == query.RegisterId)
               && (query.ExpenseItemId is null || l.StoreExpenseItemId == query.ExpenseItemId)
               && (string.IsNullOrEmpty(payee) || (l.Payee ?? string.Empty).Contains(payee, StringComparison.OrdinalIgnoreCase));
        if (query.RegisterId is not null || query.ExpenseItemId is not null || !string.IsNullOrEmpty(payee))
            docs = docs.Where(d => d.Lines.Any(LineMatches)).ToList();

        var names = await _store.GetUserNamesAsync(docs.Select(d => d.CreatedByUserId).OfType<int>(), ct);
        var items = new List<StoreExpenseResponse>();
        foreach (var d in docs)
            items.Add(await MapExpenseAsync(d, names));

        var live = docs.Where(d => d.CancelledAtUtc == null).SelectMany(d => d.Lines).Where(LineMatches).ToList();
        return new StoreExpenseListResponse
        {
            Total = R2(live.Sum(l => l.Amount)),
            ByItem = live.GroupBy(l => l.StoreExpenseItem?.Name ?? string.Empty)
                .Select(g => new StoreExpenseItemTotal { Name = g.Key, Amount = R2(g.Sum(l => l.Amount)) })
                .OrderByDescending(x => x.Amount).ToList(),
            Items = items,
        };
    }

    // ---- Create -----------------------------------------------------------------------------

    public async Task<StoreExpenseResponse> Handle(CreateStoreExpenseCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;
        var dto = command.Request;

        if (dto.Lines.Count == 0)
            throw new BadRequestException("Ən azı bir xərc sətri yazılmalıdır.");

        var itemsById = (await _expenses.GetItemsAsync(companyId, true, ct)).ToDictionary(i => i.Id);
        var shifts = new Dictionary<int, StoreCashShift>();
        var cashLeft = new Dictionary<int, decimal>();
        var prepared = new List<(CreateStoreExpenseLineRequest Line, decimal Amount, StoreCashShift Shift, StoreExpenseItem Item, StoreCashRegister Register)>();

        foreach (var (line, index) in dto.Lines.Select((l, i) => (l, i + 1)))
        {
            var amount = R2(line.Amount);
            if (amount <= 0)
                throw new BadRequestException($"{index}-ci sətirdə məbləğ 0-dan böyük olmalıdır.");
            if (!itemsById.TryGetValue(line.ExpenseItemId, out var item) || !item.IsActive)
                throw new BadRequestException($"{index}-ci sətirdə xərc maddəsi seçilməyib.");
            var register = await _store.GetRegisterAsync(line.RegisterId, companyId, ct)
                ?? throw new BadRequestException($"{index}-ci sətirdə kassa seçilməyib.");

            if (!shifts.TryGetValue(register.Id, out var shift))
            {
                shift = await _store.GetOpenShiftForRegisterAsync(register.Id, ct)
                    ?? throw new BadRequestException($"“{register.Name}” kassasında açıq növbə yoxdur: xərc növbənin kassasından çıxır. Əvvəlcə növbəni açın.");
                shifts[register.Id] = shift;
                cashLeft[register.Id] = (await _mediator.Send(new GetStoreShiftReportQuery(shift.Id), ct)).ExpectedCash;
            }

            // The money has to be in the till; several lines of the same till add up.
            if (amount > cashLeft[register.Id])
                throw new BadRequestException($"“{register.Name}” kassasında bu qədər nağd yoxdur (var: {cashLeft[register.Id]:0.00}).");
            cashLeft[register.Id] -= amount;

            prepared.Add((line, amount, shift, item, register));
        }

        var expense = new StoreExpense
        {
            CompanyId = companyId,
            DocumentNo = Guid.NewGuid().ToString("N"),
            DocumentDate = DateTime.UtcNow,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
            CreatedByUserId = userId,
        };

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _expenses.AddExpenseAsync(expense, ct);
            await _expenses.SaveChangesAsync(ct);
            expense.DocumentNo = $"XR-{expense.Id:D7}";

            var movements = new List<(StoreExpenseLine Line, StoreCashMovement Movement)>();
            foreach (var (line, amount, shift, item, register) in prepared)
            {
                var payee = string.IsNullOrWhiteSpace(line.Payee) ? null : line.Payee.Trim();
                var expenseLine = new StoreExpenseLine
                {
                    StoreExpenseId = expense.Id,
                    StoreCashRegisterId = register.Id,
                    StoreCashShiftId = shift.Id,
                    StoreExpenseItemId = item.Id,
                    Payee = payee,
                    Amount = amount,
                    Note = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim(),
                    CreatedByUserId = userId,
                };
                expense.Lines.Add(expenseLine);

                var movement = new StoreCashMovement
                {
                    CompanyId = companyId,
                    StoreCashShiftId = shift.Id,
                    Type = StoreCashMovementType.Out,
                    Amount = amount,
                    Reason = $"Xərc {expense.DocumentNo}: {item.Name}" + (payee is null ? string.Empty : $" — {payee}"),
                    CreatedByUserId = userId,
                };
                await _store.AddCashMovementAsync(movement, ct);
                movements.Add((expenseLine, movement));
            }

            await _expenses.SaveChangesAsync(ct);
            foreach (var (line, movement) in movements)
                line.StoreCashMovementId = movement.Id;
            await _expenses.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        var created = await _expenses.GetExpenseAsync(expense.Id, companyId, ct) ?? expense;
        return await MapExpenseAsync(created, await _store.GetUserNamesAsync(new[] { userId }, ct));
    }

    // ---- Cancel -----------------------------------------------------------------------------

    public async Task<StoreExpenseResponse> Handle(CancelStoreExpenseCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;

        var expense = await _expenses.GetExpenseAsync(command.Id, companyId, ct) ?? throw new NotFoundException("Xərc sənədi tapılmadı.");
        if (expense.CancelledAtUtc != null)
            throw new BadRequestException("Bu sənəd artıq ləğv edilib.");
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new BadRequestException("Ləğvin səbəbi yazılmalıdır.");

        // The money goes back into each till's open shift.
        var shifts = new Dictionary<int, StoreCashShift>();
        foreach (var registerId in expense.Lines.Select(l => l.StoreCashRegisterId).Distinct())
        {
            var register = expense.Lines.First(l => l.StoreCashRegisterId == registerId).StoreCashRegister;
            shifts[registerId] = await _store.GetOpenShiftForRegisterAsync(registerId, ct)
                ?? throw new BadRequestException($"Ləğv üçün “{register?.Name}” kassasında açıq növbə olmalıdır: pul növbənin kassasına qayıdır.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var reversals = new List<(StoreExpenseLine Line, StoreCashMovement Movement)>();
            foreach (var line in expense.Lines)
            {
                var movement = new StoreCashMovement
                {
                    CompanyId = companyId,
                    StoreCashShiftId = shifts[line.StoreCashRegisterId].Id,
                    Type = StoreCashMovementType.In,
                    Amount = line.Amount,
                    Reason = $"Xərc ləğvi {expense.DocumentNo}: {command.Reason.Trim()}",
                    CreatedByUserId = userId,
                };
                await _store.AddCashMovementAsync(movement, ct);
                reversals.Add((line, movement));
            }

            expense.CancelledAtUtc = DateTime.UtcNow;
            expense.CancelledByUserId = userId;
            expense.CancelReason = command.Reason.Trim();
            await _expenses.SaveChangesAsync(ct);

            foreach (var (line, movement) in reversals)
                line.ReversalMovementId = movement.Id;
            await _expenses.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return await MapExpenseAsync(expense, await _store.GetUserNamesAsync(expense.CreatedByUserId is { } u ? new[] { u } : Array.Empty<int>(), ct));
    }
}
