using Domain.Entities.WarehouseAndStock;
using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

/// <summary>Non-food goods the business keeps (plates, glasses, cutlery, detergent): their stock and what happened to it.</summary>
public interface IStockAssetRepository
{
    /// <summary>
    /// The company's stock items with their category and the balance in each warehouse: only the non-food ones, or every item when
    /// <paramref name="allTypes"/> is set (the stock page).
    /// </summary>
    Task<List<Domain.Entities.WarehouseAndStock.StockItem>> GetAssetItemsAsync(int companyId, bool allTypes, CancellationToken cancellationToken);

    /// <summary>
    /// Stock movements inside a period, newest first: of one item (any kind of item) when <paramref name="stockItemId"/> is given,
    /// else of the non-food items.
    /// </summary>
    Task<List<StockMovement>> GetAssetMovementsAsync(
        int companyId, DateTime? fromUtc, DateTime? toUtcExclusive, int? stockItemId, CancellationToken cancellationToken);

    /// <summary>Why the goods of the given write-offs were written off.</summary>
    Task<Dictionary<int, StockWriteOffReason>> GetWriteOffReasonsAsync(IEnumerable<int> writeOffIds, CancellationToken cancellationToken);
}
