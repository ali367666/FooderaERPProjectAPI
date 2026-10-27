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

    /// <summary>Saves; false when someone else changed the purchase (its status or paid amount) in the meantime.</summary>
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> WarehouseExistsAsync(int companyId, int warehouseId, CancellationToken cancellationToken = default);
    Task<bool> CounterpartyExistsAsync(int companyId, int counterpartyId, CancellationToken cancellationToken = default);

    /// <summary>How many of these stock items exist in the company.</summary>
    Task<int> CountStockItemsAsync(int companyId, IReadOnlyCollection<int> stockItemIds, CancellationToken cancellationToken = default);

    /// <summary>Tracked stock items, so their cost fields can be updated.</summary>
    Task<List<Domain.Entities.WarehouseAndStock.StockItem>> GetStockItemsAsync(
        int companyId, IReadOnlyCollection<int> stockItemIds, CancellationToken cancellationToken = default);

    /// <summary>Quantity of the item across every warehouse of the company, as currently saved.</summary>
    Task<decimal> GetTotalStockQuantityAsync(int companyId, int stockItemId, CancellationToken cancellationToken = default);
}
