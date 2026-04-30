using WarehouseStockRow = Domain.Entities.WarehouseAndStock.WarehouseStock;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IWarehouseStockRepository
{
    Task<WarehouseStockRow?> GetByWarehouseAndStockItemAsync(
        int companyId,
        int warehouseId,
        int stockItemId,
        CancellationToken cancellationToken);

    Task<List<WarehouseStockRow>> SearchAsync(
        int companyId,
        int? warehouseId,
        int? stockItemId,
        string? search,
        CancellationToken cancellationToken);

    Task AddAsync(WarehouseStockRow warehouseStock, CancellationToken cancellationToken);

    void Update(WarehouseStockRow warehouseStock);

    /// <summary>Tracked row with Quantity 0 if none existed (for receive / positive adjustments).</summary>
    Task<WarehouseStockRow> GetOrCreateZeroBalanceAsync(
        int companyId,
        int warehouseId,
        int stockItemId,
        int unitId,
        int? createdByUserId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
