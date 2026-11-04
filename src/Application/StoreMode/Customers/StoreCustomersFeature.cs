using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Customers;

public record SearchStoreCustomersQuery(string? Term, bool IncludeInactive) : IRequest<List<StoreCustomerResponse>>;

/// <summary>The till's customer lookup: by code or phone (or the only name match).</summary>
public record FindStoreCustomerQuery(string Term) : IRequest<StoreCustomerResponse>;

public record SaveStoreCustomerCommand(int? Id, SaveStoreCustomerRequest Request) : IRequest<StoreCustomerResponse>;

public record GetStoreCustomerStatementQuery(int CustomerId, DateTime? From, DateTime? To) : IRequest<StoreCustomerStatementResponse>;

public record CreateStoreCustomerPaymentCommand(CreateStoreCustomerPaymentRequest Request) : IRequest<StoreCustomerStatementResponse>;

public record AdjustStoreBonusCommand(AdjustStoreBonusRequest Request) : IRequest<StoreCustomerResponse>;

public class StoreCustomersHandlers :
    IRequestHandler<SearchStoreCustomersQuery, List<StoreCustomerResponse>>,
    IRequestHandler<FindStoreCustomerQuery, StoreCustomerResponse>,
    IRequestHandler<SaveStoreCustomerCommand, StoreCustomerResponse>,
    IRequestHandler<GetStoreCustomerStatementQuery, StoreCustomerStatementResponse>,
    IRequestHandler<CreateStoreCustomerPaymentCommand, StoreCustomerStatementResponse>,
    IRequestHandler<AdjustStoreBonusCommand, StoreCustomerResponse>
{
    private readonly IStoreCustomerRepository _customers;
    private readonly IStoreTradeRepository _trade;
    private readonly IStoreRepository _store;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    private readonly StoreBonusExpiryService _bonusExpiry;

    public StoreCustomersHandlers(
        IStoreCustomerRepository customers,
        IStoreTradeRepository trade,
        IStoreRepository store,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        StoreBonusExpiryService bonusExpiry)
    {
        _bonusExpiry = bonusExpiry;
        _customers = customers;
        _trade = trade;
        _store = store;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    private static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    // The screen shows the company's normal bonus percent to everyone; a customer's own (higher) percent is calculated for real
    // but shown only to those who may adjust bonus.
    private StoreCustomerResponse Map(StoreCustomer c, decimal debt, decimal bonus, decimal shownPercent) => new()
    {
        Id = c.Id,
        Code = c.Code,
        Name = c.Name,
        Phone = c.Phone,
        Email = c.Email,
        Note = c.Note,
        CreditLimit = c.CreditLimit,
        BonusPercentOverride = _currentUser.HasPermission(Domain.Constants.AppPermissions.StoreBonusAdjust) ? c.BonusPercentOverride : null,
        BonusPercentShown = shownPercent,
        IsActive = c.IsActive,
        Debt = debt,
        BonusBalance = bonus,
        CreditAvailable = Math.Max(0, c.CreditLimit - Math.Max(0, debt)),
    };

    private async Task<StoreCustomerResponse> MapOneAsync(StoreCustomer c, CancellationToken ct)
    {
        await _bonusExpiry.ExpireAsync(_currentUser.CompanyId, c.Id, ct);
        return Map(
            c,
            await _trade.GetLedgerBalanceAsync(_currentUser.CompanyId, StoreLedgerPartyType.Customer, c.Id, ct),
            await _customers.GetBonusBalanceAsync(_currentUser.CompanyId, c.Id, ct),
            (await _store.GetOrCreateSettingsAsync(_currentUser.CompanyId, ct)).BonusEarnPercent);
    }

    public async Task<List<StoreCustomerResponse>> Handle(SearchStoreCustomersQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var rows = await _customers.SearchCustomersAsync(companyId, query.Term, query.IncludeInactive, 100, ct);
        var debts = await _trade.GetLedgerBalancesAsync(companyId, StoreLedgerPartyType.Customer, ct);
        var bonuses = await _customers.GetBonusBalancesAsync(companyId, ct);
        var shown = (await _store.GetOrCreateSettingsAsync(companyId, ct)).BonusEarnPercent;
        return rows.Select(c => Map(c, debts.GetValueOrDefault(c.Id), bonuses.GetValueOrDefault(c.Id), shown)).ToList();
    }

    public async Task<StoreCustomerResponse> Handle(FindStoreCustomerQuery query, CancellationToken ct)
    {
        var customer = await _customers.FindCustomerAsync(_currentUser.CompanyId, query.Term ?? string.Empty, ct)
            ?? throw new NotFoundException("Bu kod və ya telefonla müştəri tapılmadı.");
        return await MapOneAsync(customer, ct);
    }

    public async Task<StoreCustomerResponse> Handle(SaveStoreCustomerCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BadRequestException("Müştərinin adı boş ola bilməz.");
        if (dto.CreditLimit < 0)
            throw new BadRequestException("Kredit limiti mənfi ola bilməz.");

        StoreCustomer customer;
        if (command.Id is { } id)
        {
            customer = await _customers.GetCustomerAsync(id, companyId, ct) ?? throw new NotFoundException("Müştəri tapılmadı.");
        }
        else
        {
            customer = new StoreCustomer { CompanyId = companyId, Code = Guid.NewGuid().ToString("N")[..20], CreatedByUserId = _currentUser.UserId };
            await _customers.AddCustomerAsync(customer, ct);
        }

        var code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim();
        if (code is not null && await _customers.CodeExistsAsync(companyId, code, command.Id, ct))
            throw new BadRequestException($"\"{code}\" kodu başqa müştərinin kartındadır.");

        string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        customer.Name = dto.Name.Trim();
        customer.Phone = Clean(dto.Phone);
        customer.Email = Clean(dto.Email);
        customer.Note = Clean(dto.Note);
        customer.CreditLimit = R2(dto.CreditLimit);

        // A higher (or lower) bonus percent for this customer is a bonus decision: it needs the bonus-adjust permission.
        var newPercent = dto.BonusPercentOverride is { } pct ? R2(pct) : (decimal?)null;
        if (newPercent is < 0 or > 100)
            throw new BadRequestException("Bonus faizi 0 ilə 100 arasında olmalıdır.");
        if (newPercent != customer.BonusPercentOverride)
        {
            if (!_currentUser.HasPermission(Domain.Constants.AppPermissions.StoreBonusAdjust))
                throw new BadRequestException("Müştərinin bonus faizini dəyişmək icazəniz yoxdur.");
            customer.BonusPercentOverride = newPercent;
        }
        customer.IsActive = dto.IsActive;
        if (code is not null)
            customer.Code = code;

        await _customers.SaveChangesAsync(ct);
        if (command.Id is null && code is null)
        {
            customer.Code = $"C-{customer.Id:D6}";
            await _customers.SaveChangesAsync(ct);
        }

        return await MapOneAsync(customer, ct);
    }

    public async Task<StoreCustomerStatementResponse> Handle(GetStoreCustomerStatementQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var customer = await _customers.GetCustomerAsync(query.CustomerId, companyId, ct) ?? throw new NotFoundException("Müştəri tapılmadı.");

        DateTime? fromUtc = query.From is { } f ? BusinessTime.ToUtc(f.Date + ReportPeriodResolver.DayStart) : null;
        DateTime? toUtc = query.To is { } t ? BusinessTime.ToUtc(t.Date.AddDays(1) + ReportPeriodResolver.DayStart) : null;

        var debtOpening = fromUtc is { } from
            ? await _trade.GetLedgerBalanceBeforeAsync(companyId, StoreLedgerPartyType.Customer, customer.Id, from, ct)
            : 0m;
        var debtRows = await _trade.GetLedgerAsync(companyId, StoreLedgerPartyType.Customer, customer.Id, fromUtc, toUtc, ct);
        var runningDebt = debtOpening;
        var debtEntries = debtRows.Select(e =>
        {
            runningDebt += e.Amount;
            return new StoreLedgerEntryResponse
            {
                Id = e.Id,
                EntryDate = e.EntryDate,
                Type = e.Type,
                TypeName = e.Type.ToString(),
                Amount = e.Amount,
                RunningBalance = runningDebt,
                DocumentType = e.DocumentType,
                DocumentId = e.DocumentId,
                DocumentNo = e.DocumentNo,
                Note = e.Note,
            };
        }).ToList();

        var bonusRows = await _customers.GetBonusEntriesAsync(companyId, customer.Id, fromUtc, toUtc, ct);
        var runningBonus = 0m;
        if (fromUtc is { } bf)
            runningBonus = (await _customers.GetBonusEntriesAsync(companyId, customer.Id, null, bf, ct)).Sum(e => e.Amount);
        var bonusEntries = bonusRows.Select(e =>
        {
            runningBonus += e.Amount;
            return new StoreBonusEntryResponse
            {
                Id = e.Id,
                CreatedAtUtc = e.CreatedAtUtc,
                Type = e.Type,
                TypeName = e.Type.ToString(),
                Amount = e.Amount,
                RunningBalance = runningBonus,
                DocumentNo = e.DocumentNo,
                Note = e.Note,
            };
        }).ToList();

        var payments = (await _customers.GetCustomerPaymentsAsync(companyId, customer.Id, ct))
            .Where(p => (fromUtc == null || p.CreatedAtUtc >= fromUtc) && (toUtc == null || p.CreatedAtUtc < toUtc))
            .Select(p => new StoreCustomerPaymentResponse
            {
                Id = p.Id,
                DocumentNo = p.DocumentNo,
                CreatedAtUtc = p.CreatedAtUtc,
                Amount = p.Amount,
                Method = p.Method,
                Reference = p.Reference,
                Note = p.Note,
                IsReversed = p.ReversedAtUtc != null,
                ReverseReason = p.ReverseReason,
            }).ToList();

        return new StoreCustomerStatementResponse
        {
            Customer = await MapOneAsync(customer, ct),
            DebtEntries = debtEntries,
            BonusEntries = bonusEntries,
            Payments = payments,
        };
    }

    /// <summary>Takes a payment towards a customer's debt at the till; cash lands in the shift's cash.</summary>
    public async Task<StoreCustomerStatementResponse> Handle(CreateStoreCustomerPaymentCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;
        var dto = command.Request;

        var amount = R2(dto.Amount);
        if (amount <= 0)
            throw new BadRequestException("Ödəniş məbləği 0-dan böyük olmalıdır.");
        if (dto.Method is not (StorePaymentMethod.Cash or StorePaymentMethod.Card))
            throw new BadRequestException("Müştəri ödənişi nağd və ya kartla qəbul edilir.");

        var customer = await _customers.GetCustomerAsync(dto.CustomerId, companyId, ct) ?? throw new BadRequestException("Müştəri tapılmadı.");
        var shift = await _store.GetOpenShiftForUserAsync(companyId, userId, ct)
            ?? throw new BadRequestException("Ödəniş qəbul etmək üçün əvvəlcə növbəni açın.");

        var debt = await _trade.GetLedgerBalanceAsync(companyId, StoreLedgerPartyType.Customer, customer.Id, ct);
        if (amount > Math.Max(0, debt))
            throw new BadRequestException($"Müştərinin borcu {Math.Max(0, debt):0.00} AZN-dir, bundan çox ödəniş qəbul etmək olmur.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var payment = new StoreCustomerPayment
            {
                CompanyId = companyId,
                DocumentNo = Guid.NewGuid().ToString("N"),
                StoreCustomerId = customer.Id,
                Amount = amount,
                Method = dto.Method,
                StoreCashShiftId = shift.Id,
                Reference = string.IsNullOrWhiteSpace(dto.Reference) ? null : dto.Reference.Trim(),
                Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
                CreatedByUserId = userId,
            };
            await _customers.AddCustomerPaymentAsync(payment, ct);
            await _customers.SaveChangesAsync(ct);
            payment.DocumentNo = $"CP-{payment.Id:D7}";

            await _trade.AddLedgerEntryAsync(new StoreLedgerEntry
            {
                CompanyId = companyId,
                PartyType = StoreLedgerPartyType.Customer,
                PartyId = customer.Id,
                Type = StoreLedgerEntryType.CustomerPayment,
                Amount = -amount,
                DocumentType = "CustomerPayment",
                DocumentId = payment.Id,
                DocumentNo = payment.DocumentNo,
                Note = payment.Reference,
                CreatedByUserId = userId,
            }, ct);
            await _customers.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return await Handle(new GetStoreCustomerStatementQuery(customer.Id, null, null), ct);
    }

    public async Task<StoreCustomerResponse> Handle(AdjustStoreBonusCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;
        var amount = R2(dto.Amount);
        if (amount == 0)
            throw new BadRequestException("Düzəliş məbləği 0 ola bilməz.");
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new BadRequestException("Düzəlişin səbəbi yazılmalıdır.");

        var customer = await _customers.GetCustomerAsync(dto.CustomerId, companyId, ct) ?? throw new BadRequestException("Müştəri tapılmadı.");
        var balance = await _customers.GetBonusBalanceAsync(companyId, customer.Id, ct);
        if (balance + amount < 0)
            throw new BadRequestException($"Bonus balansı ({balance:0.00}) mənfi ola bilməz.");

        await _customers.AddBonusEntryAsync(new StoreBonusEntry
        {
            CompanyId = companyId,
            StoreCustomerId = customer.Id,
            Type = StoreBonusEntryType.Adjustment,
            Amount = amount,
            Note = dto.Reason.Trim(),
            CreatedByUserId = _currentUser.UserId,
        }, ct);
        await _customers.SaveChangesAsync(ct);
        return await MapOneAsync(customer, ct);
    }
}
