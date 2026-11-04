using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.StoreMode;
using Domain.Enums;

namespace Application.StoreMode.Trade;

/// <summary>Records a payment to a supplier, spreads it over purchases and posts it to the ledger. The caller wraps it in a transaction.</summary>
public sealed class StorePaymentService
{
    private readonly IStoreTradeRepository _trade;
    private readonly ICurrentUserService _currentUser;

    public StorePaymentService(IStoreTradeRepository trade, ICurrentUserService currentUser)
    {
        _trade = trade;
        _currentUser = currentUser;
    }

    public static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    /// <summary>What is still unpaid on each of the supplier's confirmed purchases: total minus returns minus payments.</summary>
    public async Task<(List<StorePurchase> Purchases, Dictionary<int, decimal> Paid, Dictionary<int, decimal> Returned)> LoadOpenAsync(
        int companyId, int? supplierId, CancellationToken ct)
    {
        var purchases = await _trade.GetConfirmedPurchasesAsync(companyId, supplierId, ct);
        var ids = purchases.Select(p => p.Id).ToList();
        var paid = await _trade.GetPaidByPurchaseAsync(ids, ct);
        var returned = await _trade.GetReturnedAmountByPurchaseAsync(ids, ct);
        return (purchases, paid, returned);
    }

    public static decimal Outstanding(StorePurchase p, IReadOnlyDictionary<int, decimal> paid, IReadOnlyDictionary<int, decimal> returned)
        => Math.Max(0, p.Total - returned.GetValueOrDefault(p.Id) - paid.GetValueOrDefault(p.Id));

    public async Task<StoreSupplierPayment> CreateAsync(
        StoreSupplier supplier,
        decimal amount,
        StorePaymentMethod method,
        DateTime? date,
        string? reference,
        string? note,
        List<StorePaymentAllocationRequest>? allocations,
        int? onlyPurchaseId,
        CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        amount = R2(amount);
        if (amount <= 0)
            throw new BadRequestException("Ödəniş məbləği 0-dan böyük olmalıdır.");
        if (method is not (StorePaymentMethod.Cash or StorePaymentMethod.Card or StorePaymentMethod.BankTransfer))
            throw new BadRequestException("Ödəniş üsulu yanlışdır (nağd, kart və ya bank köçürməsi).");

        var (purchases, paid, returned) = await LoadOpenAsync(companyId, supplier.Id, ct);
        var outstanding = purchases.ToDictionary(p => p.Id, p => Outstanding(p, paid, returned));

        var allocs = new List<StorePaymentAllocation>();
        if (allocations is { Count: > 0 })
        {
            foreach (var a in allocations)
            {
                if (!outstanding.TryGetValue(a.PurchaseId, out var open))
                    throw new BadRequestException("Seçilmiş alış bu təchizatçıya aid deyil və ya təsdiqlənməyib.");
                var part = R2(a.Amount);
                if (part <= 0)
                    throw new BadRequestException("Bölüşdürülən məbləğ 0-dan böyük olmalıdır.");
                if (part > open)
                    throw new BadRequestException($"Alışın qalan borcu {open:0.00} AZN-dir, bundan çox bölüşdürmək olmaz.");
                allocs.Add(new StorePaymentAllocation { StorePurchaseId = a.PurchaseId, Amount = part });
            }

            if (allocs.Sum(a => a.Amount) > amount)
                throw new BadRequestException("Bölüşdürülən məbləğ ödənişdən çoxdur.");
        }
        else
        {
            // No choice made: the oldest unpaid purchases are paid first.
            var left = amount;
            foreach (var p in purchases.Where(p => onlyPurchaseId == null || p.Id == onlyPurchaseId))
            {
                if (left <= 0) break;
                var part = Math.Min(left, outstanding[p.Id]);
                if (part <= 0) continue;
                allocs.Add(new StorePaymentAllocation { StorePurchaseId = p.Id, Amount = part });
                left -= part;
            }
        }

        var payment = new StoreSupplierPayment
        {
            CompanyId = companyId,
            DocumentNo = Guid.NewGuid().ToString("N"),
            StoreSupplierId = supplier.Id,
            Amount = amount,
            Method = method,
            PaymentDate = date ?? DateTime.UtcNow,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedByUserId = _currentUser.UserId,
            Allocations = allocs,
        };
        await _trade.AddSupplierPaymentAsync(payment, ct);
        await _trade.SaveChangesAsync(ct);
        payment.DocumentNo = $"SP-{payment.Id:D7}";

        await _trade.AddLedgerEntryAsync(new StoreLedgerEntry
        {
            CompanyId = companyId,
            PartyType = StoreLedgerPartyType.Supplier,
            PartyId = supplier.Id,
            Type = StoreLedgerEntryType.SupplierPayment,
            Amount = -amount,
            EntryDate = payment.PaymentDate,
            DocumentType = "SupplierPayment",
            DocumentId = payment.Id,
            DocumentNo = payment.DocumentNo,
            Note = payment.Reference,
            CreatedByUserId = _currentUser.UserId,
        }, ct);

        return payment;
    }

    public static StoreSupplierPaymentResponse Map(StoreSupplierPayment p)
    {
        var allocated = p.Allocations.Sum(a => a.Amount);
        return new StoreSupplierPaymentResponse
        {
            Id = p.Id,
            DocumentNo = p.DocumentNo,
            PaymentDate = p.PaymentDate,
            Amount = p.Amount,
            Method = p.Method,
            Reference = p.Reference,
            Note = p.Note,
            Allocated = allocated,
            IsReversed = p.ReversedAtUtc != null,
            ReverseReason = p.ReverseReason,
            Advance = p.Amount - allocated,
            Allocations = p.Allocations.Select(a => new StorePaymentAllocationResponse
            {
                PurchaseId = a.StorePurchaseId,
                PurchaseDocumentNo = a.StorePurchase?.DocumentNo ?? string.Empty,
                Amount = a.Amount,
            }).ToList(),
        };
    }
}
