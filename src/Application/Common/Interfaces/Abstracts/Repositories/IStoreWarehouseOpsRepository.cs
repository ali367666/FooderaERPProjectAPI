using Domain.Entities.StoreMode;
using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

/// <summary>Data access of stock counts (inventory) and warehouse-to-warehouse transfers.</summary>
public interface IStoreWarehouseOpsRepository
{
    // Inventory
    Task<StoreInventory?> GetInventoryAsync(int id, int companyId, CancellationToken ct);
    Task<List<StoreInventory>> SearchInventoriesAsync(int companyId, int? warehouseId, StoreInventoryStatus? status, CancellationToken ct);
    Task AddInventoryAsync(StoreInventory inventory, CancellationToken ct);
    Task<bool> OpenInventoryExistsAsync(int companyId, int warehouseId, int? categoryId, CancellationToken ct);

    /// <summary>Products to count: those with a balance in the warehouse, optionally of one category.</summary>
    Task<List<(StoreProduct Product, decimal Quantity)>> GetCountableProductsAsync(
        int companyId, int warehouseId, int? categoryId, CancellationToken ct);

    // Transfers
    Task<StoreTransfer?> GetTransferAsync(int id, int companyId, CancellationToken ct);
    Task<List<StoreTransfer>> SearchTransfersAsync(
        int companyId, StoreTransferStatus? status, int? warehouseId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken ct);
    Task AddTransferAsync(StoreTransfer transfer, CancellationToken ct);
    void RemoveTransferLine(StoreTransferLine line);

    Task SaveChangesAsync(CancellationToken ct);
}
