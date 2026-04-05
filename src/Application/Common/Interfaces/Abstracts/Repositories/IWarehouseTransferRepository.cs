using Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

public interface IWarehouseTransferRepository
{
    Task AddAsync(Domain.Entities.WarehouseTransfer warehouseTransfer, CancellationToken cancellationToken);
    Task<Domain.Entities.WarehouseTransfer?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Domain.Entities.WarehouseTransfer?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseTransfer>> GetAllWithDetailsAsync(CancellationToken cancellationToken);
    void Update(Domain.Entities.WarehouseTransfer warehouseTransfer);
    Task DeleteAsync(Domain.Entities.WarehouseTransfer warehouseTransfer, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<bool> WarehouseExistsAsync(int warehouseId, CancellationToken cancellationToken);
    Task<List<int>> GetExistingStockItemIdsAsync(List<int> stockItemIds, CancellationToken cancellationToken);
    void RemoveLines(IEnumerable<WarehouseTransferLine> lines);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}