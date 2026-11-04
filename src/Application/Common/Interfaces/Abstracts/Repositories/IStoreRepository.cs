using Domain.Entities;
using Domain.Entities.StoreMode;
using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

/// <summary>Data access of the store (mağaza) mode: catalog, stock ledger, tills, shifts and sales.</summary>
public interface IStoreRepository
{
    // Catalog
    Task<List<StoreCategory>> GetCategoriesAsync(int companyId, CancellationToken ct);
    Task<StoreCategory?> GetCategoryAsync(int id, int companyId, CancellationToken ct);
    Task AddCategoryAsync(StoreCategory category, CancellationToken ct);

    Task<StoreProduct?> GetProductAsync(int id, int companyId, CancellationToken ct);
    Task<List<StoreProduct>> GetProductsByIdsAsync(int companyId, IReadOnlyCollection<int> ids, CancellationToken ct);

    /// <summary>The active product with this scan code or SKU, or null.</summary>
    Task<StoreProduct?> FindProductByCodeAsync(int companyId, string code, CancellationToken ct);

    Task<(List<StoreProduct> Items, int Total)> SearchProductsAsync(
        int companyId, string? term, int? categoryId, bool? isActive, int skip, int take, CancellationToken ct);

    Task<bool> BarcodeTakenAsync(int companyId, string barcode, int? exceptProductId, CancellationToken ct);
    Task<bool> SkuTakenAsync(int companyId, string sku, int? exceptProductId, CancellationToken ct);
    Task AddProductAsync(StoreProduct product, CancellationToken ct);

    // Stock
    Task<StoreSettings> GetOrCreateSettingsAsync(int companyId, CancellationToken ct);
    Task<StoreStockBalance> GetOrCreateBalanceAsync(int companyId, int warehouseId, int productId, CancellationToken ct);
    Task<Dictionary<int, StoreStockBalance>> GetBalancesAsync(int companyId, int warehouseId, IReadOnlyCollection<int> productIds, CancellationToken ct);
    Task<decimal> GetTotalQuantityAsync(int companyId, int productId, CancellationToken ct);
    Task<Dictionary<int, decimal>> GetWarehouseQuantitiesAsync(int companyId, int warehouseId, IReadOnlyCollection<int> productIds, CancellationToken ct);
    Task<List<StoreStockBalance>> SearchBalancesAsync(int companyId, int? warehouseId, string? term, bool lowOnly, CancellationToken ct);
    Task AddMovementAsync(StoreStockMovement movement, CancellationToken ct);
    Task<List<StoreStockMovement>> SearchMovementsAsync(
        int companyId, int? warehouseId, int? productId, DateTime? fromUtc, DateTime? toUtcExclusive, int take, CancellationToken ct);

    // Tills and shifts
    Task<List<StoreCashRegister>> GetRegistersAsync(int companyId, int? restaurantId, CancellationToken ct);
    Task<StoreCashRegister?> GetRegisterAsync(int id, int companyId, CancellationToken ct);
    Task AddRegisterAsync(StoreCashRegister register, CancellationToken ct);
    Task<bool> WarehouseInBranchAsync(int companyId, int warehouseId, int restaurantId, CancellationToken ct);
    Task<bool> WarehouseExistsAsync(int companyId, int warehouseId, CancellationToken ct);
    Task<bool> BranchExistsAsync(int companyId, int restaurantId, CancellationToken ct);

    Task<StoreCashShift?> GetShiftAsync(int id, int companyId, CancellationToken ct);
    Task<StoreCashShift?> GetOpenShiftForRegisterAsync(int registerId, CancellationToken ct);
    Task<StoreCashShift?> GetOpenShiftForUserAsync(int companyId, int userId, CancellationToken ct);
    Task<List<StoreCashShift>> GetOpenShiftsAsync(int companyId, IReadOnlyCollection<int> registerIds, CancellationToken ct);
    Task<List<StoreCashShift>> SearchShiftsAsync(int companyId, int? registerId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken ct);
    Task AddShiftAsync(StoreCashShift shift, CancellationToken ct);
    Task AddCashMovementAsync(StoreCashMovement movement, CancellationToken ct);
    Task<List<StoreCashMovement>> GetCashMovementsAsync(int shiftId, CancellationToken ct);

    // Sales
    Task<StoreSale?> GetSaleByOperationAsync(int companyId, Guid operationId, CancellationToken ct);
    Task<StoreSale?> GetSaleAsync(int id, int companyId, CancellationToken ct);
    Task<List<StoreSale>> GetSalesForShiftAsync(int shiftId, CancellationToken ct);
    Task<(List<StoreSale> Items, int Total)> SearchSalesAsync(
        int companyId, DateTime? fromUtc, DateTime? toUtcExclusive, int? shiftId, StoreSaleStatus? status, string? documentNo,
        int skip, int take, IReadOnlyCollection<int>? branchIds, CancellationToken ct);
    Task AddSaleAsync(StoreSale sale, CancellationToken ct);

    // Quick slots and held sales
    Task<List<StoreQuickSlot>> GetQuickSlotsAsync(int registerId, CancellationToken ct);
    Task AddQuickSlotAsync(StoreQuickSlot slot, CancellationToken ct);
    void RemoveQuickSlot(StoreQuickSlot slot);
    Task<List<StoreHeldSale>> GetHeldSalesAsync(int companyId, int registerId, CancellationToken ct);
    Task<StoreHeldSale?> GetHeldSaleAsync(int id, int companyId, CancellationToken ct);
    Task AddHeldSaleAsync(StoreHeldSale held, CancellationToken ct);
    void RemoveHeldSale(StoreHeldSale held);

    // Serial / IMEI units
    Task<List<StoreSerialUnit>> FindSerialUnitsAsync(int companyId, IReadOnlyCollection<string> serialNumbers, CancellationToken ct);
    Task<List<StoreSerialUnit>> GetSerialUnitsForSaleLinesAsync(IReadOnlyCollection<int> saleLineIds, CancellationToken ct);
    Task<List<StoreSerialUnit>> SearchSerialUnitsAsync(
        int companyId, string? term, int? productId, int? warehouseId, StoreSerialStatus? status, int take, CancellationToken ct);
    Task<StoreSerialUnit?> GetSerialUnitWithHistoryAsync(int id, int companyId, CancellationToken ct);
    Task AddSerialUnitAsync(StoreSerialUnit unit, CancellationToken ct);
    Task AddSerialEventAsync(StoreSerialEvent serialEvent, CancellationToken ct);

    // Approvals and returns
    Task AddApprovalLogAsync(StoreApprovalLog log, CancellationToken ct);
    Task<StoreSale?> GetSaleByDocumentNoAsync(int companyId, string documentNo, CancellationToken ct);
    Task<StoreSaleReturn?> GetReturnByOperationAsync(int companyId, Guid operationId, CancellationToken ct);
    Task<StoreSaleReturn?> GetReturnAsync(int id, int companyId, CancellationToken ct);
    Task<List<StoreSaleReturn>> GetReturnsForSaleAsync(int saleId, CancellationToken ct);
    Task<List<StoreSaleReturn>> GetReturnsForShiftAsync(int shiftId, CancellationToken ct);
    Task<List<StoreSaleReturn>> SearchReturnsAsync(int companyId, DateTime? fromUtc, DateTime? toUtcExclusive, int take, CancellationToken ct);
    Task AddReturnAsync(StoreSaleReturn saleReturn, CancellationToken ct);

    Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
