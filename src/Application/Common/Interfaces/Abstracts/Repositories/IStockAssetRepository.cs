using Domain.Entities.WarehouseAndStock;
using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

/// <summary>Non-food goods the business keeps (plates, glasses, cutlery, detergent): their stock and what happened to it.</summary>
public interface IStockAssetRepository
{
    /// <summary>The company's non-food stock items with their category and the balance in each warehouse.</summary>
    Task<List<Domain.Entities.WarehouseAndStock.StockItem>> GetAssetItemsAsync(int companyId, CancellationToken cancellationToken);

    /// <summary>Stock movements of non-food items inside a period, newest first.</summary>
    Task<List<StockMovement>> GetAssetMovementsAsync(
        int companyId, DateTime? fromUtc, DateTime? toUtcExclusive, int? stockItemId, CancellationToken cancellationToken);

    /// <summary>Why the goods of the given write-offs were written off.</summary>
    Task<Dictionary<int, StockWriteOffReason>> GetWriteOffReasonsAsync(IEnumerable<int> writeOffIds, CancellationToken cancellationToken);
}
