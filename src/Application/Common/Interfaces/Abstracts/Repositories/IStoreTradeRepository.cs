using Domain.Entities.StoreMode;
using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

/// <summary>Data access of the store's buying side: suppliers, purchases, supplier payments, the ledger and price history.</summary>
public interface IStoreTradeRepository
{
    /// <summary>For each product, the supplier of its latest confirmed purchase (products never purchased are absent).</summary>
    Task<Dictionary<int, string>> GetLastSupplierNamesAsync(int companyId, IReadOnlyCollection<int> productIds, CancellationToken ct);

    // Suppliers
    Task<List<StoreSupplier>> GetSuppliersAsync(int companyId, bool includeInactive, CancellationToken ct);
    Task<StoreSupplier?> GetSupplierAsync(int id, int companyId, CancellationToken ct);
    Task AddSupplierAsync(StoreSupplier supplier, CancellationToken ct);

    // Ledger
    Task AddLedgerEntryAsync(StoreLedgerEntry entry, CancellationToken ct);
    Task<StoreSupplierPayment?> GetSupplierPaymentAsync(int id, int companyId, CancellationToken ct);
    Task<decimal> GetLedgerBalanceAsync(int companyId, StoreLedgerPartyType partyType, int partyId, CancellationToken ct);
    Task<Dictionary<int, decimal>> GetLedgerBalancesAsync(int companyId, StoreLedgerPartyType partyType, CancellationToken ct);
    Task<List<StoreLedgerEntry>> GetLedgerAsync(
        int companyId, StoreLedgerPartyType partyType, int partyId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken ct);
    Task<decimal> GetLedgerBalanceBeforeAsync(
        int companyId, StoreLedgerPartyType partyType, int partyId, DateTime beforeUtc, CancellationToken ct);

    // Purchases
    Task<StorePurchase?> GetPurchaseAsync(int id, int companyId, CancellationToken ct);
    Task<(List<StorePurchase> Items, int Total)> SearchPurchasesAsync(
        int companyId, int? supplierId, StorePurchaseStatus? status, DateTime? fromUtc, DateTime? toUtcExclusive,
        string? term, int skip, int take, CancellationToken ct, IReadOnlyCollection<int>? warehouseIds = null);
    Task AddPurchaseAsync(StorePurchase purchase, CancellationToken ct);
    void RemovePurchaseLine(StorePurchaseLine line);

    /// <summary>What has been paid against each purchase (allocated supplier payments).</summary>
    Task<Dictionary<int, decimal>> GetPaidByPurchaseAsync(IReadOnlyCollection<int> purchaseIds, CancellationToken ct);

    /// <summary>What has been handed back to the supplier from each purchase (AZN).</summary>
    Task<Dictionary<int, decimal>> GetReturnedAmountByPurchaseAsync(IReadOnlyCollection<int> purchaseIds, CancellationToken ct);
    Task<List<StorePurchaseReturn>> GetReturnsForPurchaseAsync(int purchaseId, CancellationToken ct);
    Task AddPurchaseReturnAsync(StorePurchaseReturn purchaseReturn, CancellationToken ct);

    /// <summary>The supplier's confirmed purchases, oldest first.</summary>
    Task<List<StorePurchase>> GetConfirmedPurchasesAsync(int companyId, int? supplierId, CancellationToken ct);

    // Payments
    Task AddSupplierPaymentAsync(StoreSupplierPayment payment, CancellationToken ct);
    Task<List<StoreSupplierPayment>> GetSupplierPaymentsAsync(int companyId, int supplierId, CancellationToken ct);

    // Price history
    Task AddPriceHistoryAsync(StorePriceHistory entry, CancellationToken ct);
    Task<List<StorePriceHistory>> GetPriceHistoryAsync(int companyId, int productId, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
