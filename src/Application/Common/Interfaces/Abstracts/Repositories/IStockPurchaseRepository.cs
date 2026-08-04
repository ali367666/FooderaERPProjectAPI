using StockPurchaseEntity = Domain.Entities.WarehouseAndStock.StockPurchase;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IStockPurchaseRepository
{
    Task<List<StockPurchaseEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<StockPurchaseEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(StockPurchaseEntity purchase, CancellationToken cancellationToken = default);
    void Update(StockPurchaseEntity purchase);
    void Remove(StockPurchaseEntity purchase);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
