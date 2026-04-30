using Domain.Entities.WarehouseAndStock;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IStockMovementRepository
{
    Task AddAsync(StockMovement stockMovement, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<StockMovement> stockMovements, CancellationToken cancellationToken);

    Task<List<StockMovement>> GetByWarehouseTransferIdAsync(int warehouseTransferId, CancellationToken cancellationToken);

    Task<List<StockMovement>> SearchByCompanyAsync(
        int companyId,
        string? search,
        CancellationToken cancellationToken);

    Task<decimal> GetStockBalanceAsync(int warehouseId, int stockItemId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}