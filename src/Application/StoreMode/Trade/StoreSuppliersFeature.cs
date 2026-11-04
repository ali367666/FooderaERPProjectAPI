using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Trade;

public record GetStoreSuppliersQuery(bool IncludeInactive) : IRequest<List<StoreSupplierResponse>>;

public record SaveStoreSupplierCommand(int? Id, SaveStoreSupplierRequest Request) : IRequest<StoreSupplierResponse>;

public record GetStoreSupplierStatementQuery(int SupplierId, DateTime? From, DateTime? To) : IRequest<StoreSupplierStatementResponse>;

public record CreateStoreSupplierPaymentCommand(CreateStoreSupplierPaymentRequest Request) : IRequest<StoreSupplierPaymentResponse>;

public record GetStoreSupplierDebtReportQuery : IRequest<StoreSupplierDebtReportResponse>;

public record GetStorePriceHistoryQuery(int ProductId) : IRequest<List<StorePriceHistoryResponse>>;

public class StoreSuppliersHandlers :
    IRequestHandler<GetStoreSuppliersQuery, List<StoreSupplierResponse>>,
    IRequestHandler<SaveStoreSupplierCommand, StoreSupplierResponse>,
    IRequestHandler<GetStoreSupplierStatementQuery, StoreSupplierStatementResponse>,
    IRequestHandler<CreateStoreSupplierPaymentCommand, StoreSupplierPaymentResponse>,
    IRequestHandler<GetStoreSupplierDebtReportQuery, StoreSupplierDebtReportResponse>,
    IRequestHandler<GetStorePriceHistoryQuery, List<StorePriceHistoryResponse>>
{
    private readonly IStoreTradeRepository _trade;
    private readonly IStoreRepository _store;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StorePaymentService _payments;

    public StoreSuppliersHandlers(
        IStoreTradeRepository trade,
        IStoreRepository store,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        StorePaymentService payments)
    {
        _trade = trade;
        _store = store;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _payments = payments;
    }

    private static StoreSupplierResponse Map(StoreSupplier s, decimal balance) => new()
    {
        Id = s.Id,
        Name = s.Name,
        LegalName = s.LegalName,
        Phone = s.Phone,
        Email = s.Email,
        Address = s.Address,
        TaxNumber = s.TaxNumber,
        Note = s.Note,
        PaymentTermDays = s.PaymentTermDays,
        IsActive = s.IsActive,
        Balance = balance,
    };

    public async Task<List<StoreSupplierResponse>> Handle(GetStoreSuppliersQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var suppliers = await _trade.GetSuppliersAsync(companyId, query.IncludeInactive, ct);
        var balances = await _trade.GetLedgerBalancesAsync(companyId, StoreLedgerPartyType.Supplier, ct);
        return suppliers.Select(s => Map(s, balances.GetValueOrDefault(s.Id))).ToList();
    }

    public async Task<StoreSupplierResponse> Handle(SaveStoreSupplierCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BadRequestException("Təchizatçının adı boş ola bilməz.");
        if (dto.PaymentTermDays < 0)
            throw new BadRequestException("Ödəniş müddəti mənfi ola bilməz.");

        StoreSupplier supplier;
        var isNew = command.Id is null;
        if (command.Id is { } id)
        {
            supplier = await _trade.GetSupplierAsync(id, companyId, ct) ?? throw new NotFoundException("Təchizatçı tapılmadı.");
        }
        else
        {
            supplier = new StoreSupplier { CompanyId = companyId, CreatedByUserId = _currentUser.UserId };
            await _trade.AddSupplierAsync(supplier, ct);
        }

        string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        supplier.Name = dto.Name.Trim();
        supplier.LegalName = Clean(dto.LegalName);
        supplier.Phone = Clean(dto.Phone);
        supplier.Email = Clean(dto.Email);
        supplier.Address = Clean(dto.Address);
        supplier.TaxNumber = Clean(dto.TaxNumber);
        supplier.Note = Clean(dto.Note);
        supplier.PaymentTermDays = dto.PaymentTermDays;
        supplier.IsActive = dto.IsActive;
        await _trade.SaveChangesAsync(ct);

        // What the store already owed before the system: the first ledger entry of the account.
        if (isNew && dto.OpeningBalance is { } opening && opening != 0)
        {
            await _trade.AddLedgerEntryAsync(new StoreLedgerEntry
            {
                CompanyId = companyId,
                PartyType = StoreLedgerPartyType.Supplier,
                PartyId = supplier.Id,
                Type = StoreLedgerEntryType.OpeningBalance,
                Amount = StorePaymentService.R2(opening),
                Note = "Başlanğıc qalıq",
                CreatedByUserId = _currentUser.UserId,
            }, ct);
            await _trade.SaveChangesAsync(ct);
        }

        return Map(supplier, await _trade.GetLedgerBalanceAsync(companyId, StoreLedgerPartyType.Supplier, supplier.Id, ct));
    }

    public async Task<StoreSupplierStatementResponse> Handle(GetStoreSupplierStatementQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var supplier = await _trade.GetSupplierAsync(query.SupplierId, companyId, ct) ?? throw new NotFoundException("Təchizatçı tapılmadı.");

        DateTime? fromUtc = query.From is { } f ? BusinessTime.ToUtc(f.Date + ReportPeriodResolver.DayStart) : null;
        DateTime? toUtc = query.To is { } t ? BusinessTime.ToUtc(t.Date.AddDays(1) + ReportPeriodResolver.DayStart) : null;

        var opening = fromUtc is { } from
            ? await _trade.GetLedgerBalanceBeforeAsync(companyId, StoreLedgerPartyType.Supplier, supplier.Id, from, ct)
            : 0m;
        var entries = await _trade.GetLedgerAsync(companyId, StoreLedgerPartyType.Supplier, supplier.Id, fromUtc, toUtc, ct);

        var running = opening;
        var rows = new List<StoreLedgerEntryResponse>();
        foreach (var e in entries)
        {
            running += e.Amount;
            rows.Add(new StoreLedgerEntryResponse
            {
                Id = e.Id,
                EntryDate = e.EntryDate,
                Type = e.Type,
                TypeName = e.Type.ToString(),
                Amount = e.Amount,
                RunningBalance = running,
                DocumentType = e.DocumentType,
                DocumentId = e.DocumentId,
                DocumentNo = e.DocumentNo,
                Note = e.Note,
            });
        }

        var payments = (await _trade.GetSupplierPaymentsAsync(companyId, supplier.Id, ct))
            .Where(p => (fromUtc == null || p.PaymentDate >= fromUtc) && (toUtc == null || p.PaymentDate < toUtc))
            .Select(StorePaymentService.Map).ToList();

        return new StoreSupplierStatementResponse
        {
            Supplier = Map(supplier, await _trade.GetLedgerBalanceAsync(companyId, StoreLedgerPartyType.Supplier, supplier.Id, ct)),
            OpeningBalance = opening,
            TotalIncrease = entries.Where(e => e.Amount > 0).Sum(e => e.Amount),
            TotalDecrease = -entries.Where(e => e.Amount < 0).Sum(e => e.Amount),
            ClosingBalance = running,
            Entries = rows,
            Payments = payments,
        };
    }

    public async Task<StoreSupplierPaymentResponse> Handle(CreateStoreSupplierPaymentCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;
        var supplier = await _trade.GetSupplierAsync(dto.SupplierId, companyId, ct) ?? throw new BadRequestException("Təchizatçı tapılmadı.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var payment = await _payments.CreateAsync(
                supplier, dto.Amount, dto.Method, dto.PaymentDate, dto.Reference, dto.Note, dto.Allocations, null, ct);
            await _trade.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            // Allocations carry the purchase numbers for the answer.
            var payments = await _trade.GetSupplierPaymentsAsync(companyId, supplier.Id, ct);
            return StorePaymentService.Map(payments.First(p => p.Id == payment.Id));
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>Who we owe, how much of it is overdue (aging) and the unpaid purchases behind it.</summary>
    public async Task<StoreSupplierDebtReportResponse> Handle(GetStoreSupplierDebtReportQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var suppliers = await _trade.GetSuppliersAsync(companyId, true, ct);
        var balances = await _trade.GetLedgerBalancesAsync(companyId, StoreLedgerPartyType.Supplier, ct);
        var (purchases, paid, returned) = await _payments.LoadOpenAsync(companyId, null, ct);
        var today = DateTime.UtcNow.Date;

        var rows = new List<StoreSupplierDebtResponse>();
        foreach (var s in suppliers)
        {
            var balance = balances.GetValueOrDefault(s.Id);
            var open = purchases.Where(p => p.StoreSupplierId == s.Id)
                .Select(p => (Purchase: p, Open: StorePaymentService.Outstanding(p, paid, returned)))
                .Where(x => x.Open > 0)
                .ToList();
            if (balance == 0 && open.Count == 0)
                continue;

            var row = new StoreSupplierDebtResponse { SupplierId = s.Id, SupplierName = s.Name, Balance = balance, Advance = Math.Max(0, -balance) };
            foreach (var (p, amount) in open)
            {
                var due = (p.DueDate ?? p.PurchaseDate.AddDays(s.PaymentTermDays)).Date;
                var days = (today - due).Days;
                if (days <= 0) row.NotDue += amount;
                else if (days <= 30) row.Overdue1To30 += amount;
                else if (days <= 60) row.Overdue31To60 += amount;
                else if (days <= 90) row.Overdue61To90 += amount;
                else row.Overdue90Plus += amount;

                row.OpenPurchases.Add(new StoreOpenPurchaseResponse
                {
                    PurchaseId = p.Id,
                    DocumentNo = p.DocumentNo,
                    SupplierInvoiceNo = p.SupplierInvoiceNo,
                    PurchaseDate = p.PurchaseDate,
                    DueDate = due,
                    Total = p.Total,
                    Paid = paid.GetValueOrDefault(p.Id),
                    Returned = returned.GetValueOrDefault(p.Id),
                    Outstanding = amount,
                    DaysOverdue = Math.Max(0, days),
                });
            }

            var payments = await _trade.GetSupplierPaymentsAsync(companyId, s.Id, ct);
            row.LastPaymentDate = payments.Select(p => (DateTime?)p.PaymentDate).FirstOrDefault();
            rows.Add(row);
        }

        return new StoreSupplierDebtReportResponse
        {
            TotalOwed = rows.Sum(r => Math.Max(0, r.Balance)),
            TotalOverdue = rows.Sum(r => r.TotalOverdue),
            TotalAdvance = rows.Sum(r => r.Advance),
            Suppliers = rows.OrderByDescending(r => r.TotalOverdue).ThenByDescending(r => r.Balance).ToList(),
        };
    }

    public async Task<List<StorePriceHistoryResponse>> Handle(GetStorePriceHistoryQuery query, CancellationToken ct)
    {
        var rows = await _trade.GetPriceHistoryAsync(_currentUser.CompanyId, query.ProductId, ct);
        var names = await _store.GetUserNamesAsync(rows.Select(r => r.CreatedByUserId).OfType<int>(), ct);
        return rows.Select(r => new StorePriceHistoryResponse
        {
            PriceType = r.PriceType,
            PriceTypeName = r.PriceType.ToString(),
            OldPrice = r.OldPrice,
            NewPrice = r.NewPrice,
            EffectiveFromUtc = r.EffectiveFromUtc,
            ChangedByName = r.CreatedByUserId is { } id && names.TryGetValue(id, out var n) ? n : null,
            Reason = r.Reason,
        }).ToList();
    }
}
