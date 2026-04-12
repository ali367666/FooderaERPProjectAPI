using Domain.Entities.WarehouseAndStock;
using Microsoft.EntityFrameworkCore.Storage;

public interface IWarehouseTransferRepository
{
    Task AddAsync(WarehouseTransfer warehouseTransfer, CancellationToken cancellationToken);
    Task<WarehouseTransfer?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<WarehouseTransfer?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken);
    Task<List<WarehouseTransfer>> GetAllWithDetailsAsync(CancellationToken cancellationToken);
    void Update(WarehouseTransfer warehouseTransfer);
    Task DeleteAsync(WarehouseTransfer warehouseTransfer, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<bool> WarehouseExistsAsync(int warehouseId, CancellationToken cancellationToken);
    Task<List<int>> GetExistingStockItemIdsAsync(List<int> stockItemIds, CancellationToken cancellationToken);
    void RemoveLines(IEnumerable<WarehouseTransferLine> lines);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}