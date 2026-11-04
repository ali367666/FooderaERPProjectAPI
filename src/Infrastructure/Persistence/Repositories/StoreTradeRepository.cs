using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.StoreMode;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StoreTradeRepository : IStoreTradeRepository
{
    private readonly AppDbContext _context;

    public StoreTradeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<int, string>> GetLastSupplierNamesAsync(int companyId, IReadOnlyCollection<int> productIds, CancellationToken ct)
    {
        if (productIds.Count == 0) return new();
        var rows = await _context.StorePurchaseLines.AsNoTracking()
            .Where(l => productIds.Contains(l.StoreProductId)
                && l.StorePurchase.CompanyId == companyId
                && l.StorePurchase.Status == Domain.Enums.StorePurchaseStatus.Confirmed)
            .Select(l => new { l.StoreProductId, SupplierName = l.StorePurchase.StoreSupplier.Name, l.StorePurchase.PurchaseDate, l.StorePurchaseId })
            .ToListAsync(ct);
        return rows.GroupBy(r => r.StoreProductId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.PurchaseDate).ThenByDescending(r => r.StorePurchaseId).First().SupplierName);
    }

    // ---- Suppliers --------------------------------------------------------------------------

    public Task<List<StoreSupplier>> GetSuppliersAsync(int companyId, bool includeInactive, CancellationToken ct)
        => _context.StoreSuppliers.AsNoTracking()
            .Where(x => x.CompanyId == companyId && (includeInactive || x.IsActive))
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<StoreSupplier?> GetSupplierAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreSuppliers.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public async Task AddSupplierAsync(StoreSupplier supplier, CancellationToken ct)
        => await _context.StoreSuppliers.AddAsync(supplier, ct);

    // ---- Ledger -----------------------------------------------------------------------------

    public async Task AddLedgerEntryAsync(StoreLedgerEntry entry, CancellationToken ct)
        => await _context.StoreLedgerEntries.AddAsync(entry, ct);

    public async Task<decimal> GetLedgerBalanceAsync(int companyId, StoreLedgerPartyType partyType, int partyId, CancellationToken ct)
        => await _context.StoreLedgerEntries
            .Where(x => x.CompanyId == companyId && x.PartyType == partyType && x.PartyId == partyId)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

    public Task<Dictionary<int, decimal>> GetLedgerBalancesAsync(int companyId, StoreLedgerPartyType partyType, CancellationToken ct)
        => _context.StoreLedgerEntries.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.PartyType == partyType)
            .GroupBy(x => x.PartyId)
            .Select(g => new { g.Key, Sum = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Sum, ct);

    public Task<List<StoreLedgerEntry>> GetLedgerAsync(
        int companyId, StoreLedgerPartyType partyType, int partyId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken ct)
        => _context.StoreLedgerEntries.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.PartyType == partyType && x.PartyId == partyId
                && (fromUtc == null || x.EntryDate >= fromUtc)
                && (toUtcExclusive == null || x.EntryDate < toUtcExclusive))
            .OrderBy(x => x.EntryDate).ThenBy(x => x.Id)
            .ToListAsync(ct);

    public async Task<decimal> GetLedgerBalanceBeforeAsync(
        int companyId, StoreLedgerPartyType partyType, int partyId, DateTime beforeUtc, CancellationToken ct)
        => await _context.StoreLedgerEntries
            .Where(x => x.CompanyId == companyId && x.PartyType == partyType && x.PartyId == partyId && x.EntryDate < beforeUtc)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

    // ---- Purchases --------------------------------------------------------------------------

    public Task<StorePurchase?> GetPurchaseAsync(int id, int companyId, CancellationToken ct)
        => _context.StorePurchases
            .Include(x => x.StoreSupplier)
            .Include(x => x.Warehouse)
            .Include(x => x.Lines).ThenInclude(l => l.StoreProduct)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public async Task<(List<StorePurchase> Items, int Total)> SearchPurchasesAsync(
        int companyId, int? supplierId, StorePurchaseStatus? status, DateTime? fromUtc, DateTime? toUtcExclusive,
        string? term, int skip, int take, CancellationToken ct, IReadOnlyCollection<int>? warehouseIds = null)
    {
        var query = _context.StorePurchases.AsNoTracking()
            .Include(x => x.StoreSupplier)
            .Include(x => x.Warehouse)
            .Where(x => x.CompanyId == companyId
                && (supplierId == null || x.StoreSupplierId == supplierId)
                && (status == null || x.Status == status)
                && (fromUtc == null || x.PurchaseDate >= fromUtc)
                && (toUtcExclusive == null || x.PurchaseDate < toUtcExclusive)
                && (warehouseIds == null || warehouseIds.Contains(x.WarehouseId)));

        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = query.Where(x => x.DocumentNo.Contains(t) || (x.SupplierInvoiceNo != null && x.SupplierInvoiceNo.Contains(t)));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.PurchaseDate).ThenByDescending(x => x.Id).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddPurchaseAsync(StorePurchase purchase, CancellationToken ct)
        => await _context.StorePurchases.AddAsync(purchase, ct);

    public void RemovePurchaseLine(StorePurchaseLine line) => _context.StorePurchaseLines.Remove(line);

    public Task<Dictionary<int, decimal>> GetPaidByPurchaseAsync(IReadOnlyCollection<int> purchaseIds, CancellationToken ct)
        => _context.StorePaymentAllocations.AsNoTracking()
            .Where(a => purchaseIds.Contains(a.StorePurchaseId) && a.StoreSupplierPayment.ReversedAtUtc == null)
            .GroupBy(a => a.StorePurchaseId)
            .Select(g => new { g.Key, Sum = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Sum, ct);

    public Task<Dictionary<int, decimal>> GetReturnedAmountByPurchaseAsync(IReadOnlyCollection<int> purchaseIds, CancellationToken ct)
        => _context.StorePurchaseReturns.AsNoTracking()
            .Where(r => purchaseIds.Contains(r.StorePurchaseId))
            .GroupBy(r => r.StorePurchaseId)
            .Select(g => new { g.Key, Sum = g.Sum(r => r.Total) })
            .ToDictionaryAsync(x => x.Key, x => x.Sum, ct);

    public Task<List<StorePurchaseReturn>> GetReturnsForPurchaseAsync(int purchaseId, CancellationToken ct)
        => _context.StorePurchaseReturns
            .Include(x => x.Lines)
            .Where(x => x.StorePurchaseId == purchaseId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task AddPurchaseReturnAsync(StorePurchaseReturn purchaseReturn, CancellationToken ct)
        => await _context.StorePurchaseReturns.AddAsync(purchaseReturn, ct);

    public Task<List<StorePurchase>> GetConfirmedPurchasesAsync(int companyId, int? supplierId, CancellationToken ct)
        => _context.StorePurchases.AsNoTracking()
            .Include(x => x.StoreSupplier)
            .Where(x => x.CompanyId == companyId && x.Status == StorePurchaseStatus.Confirmed
                && (supplierId == null || x.StoreSupplierId == supplierId))
            .OrderBy(x => x.PurchaseDate).ThenBy(x => x.Id)
            .ToListAsync(ct);

    // ---- Payments ---------------------------------------------------------------------------

    public Task<StoreSupplierPayment?> GetSupplierPaymentAsync(int id, int companyId, CancellationToken ct)
        => _context.StoreSupplierPayments.Include(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public async Task AddSupplierPaymentAsync(StoreSupplierPayment payment, CancellationToken ct)
        => await _context.StoreSupplierPayments.AddAsync(payment, ct);

    public Task<List<StoreSupplierPayment>> GetSupplierPaymentsAsync(int companyId, int supplierId, CancellationToken ct)
        => _context.StoreSupplierPayments.AsNoTracking()
            .Include(x => x.Allocations).ThenInclude(a => a.StorePurchase)
            .Where(x => x.CompanyId == companyId && x.StoreSupplierId == supplierId)
            .OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.Id)
            .ToListAsync(ct);

    // ---- Price history ----------------------------------------------------------------------

    public async Task AddPriceHistoryAsync(StorePriceHistory entry, CancellationToken ct)
        => await _context.StorePriceHistories.AddAsync(entry, ct);

    public Task<List<StorePriceHistory>> GetPriceHistoryAsync(int companyId, int productId, CancellationToken ct)
        => _context.StorePriceHistories.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.StoreProductId == productId)
            .OrderByDescending(x => x.EffectiveFromUtc).ThenByDescending(x => x.Id)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
