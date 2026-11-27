using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

/// <summary>FIFO / LIFO costing of the store's warehouse goods: switching it on and off, the cost layers and the value they give the stock.</summary>
public interface IStoreCostingRepository
{
    /// <summary>
    /// Moves a company to a costing method. Going from the weighted average to FIFO / LIFO makes opening layers from the stock that is there now;
    /// going back drops the layers; FIFO and LIFO keep the same layers. Saved with the next SaveChanges.
    /// </summary>
    Task PrepareMethodChangeAsync(int companyId, StoreCostMethod from, StoreCostMethod to, CancellationToken ct);

    Task<StoreCostMethod> GetMethodAsync(int companyId, CancellationToken ct);

    /// <summary>The layers of the company (optionally of one warehouse / product), by product and age.</summary>
    Task<List<StoreCostLayerRow>> GetLayersAsync(int companyId, int? warehouseId, int? productId, bool onlyRemaining, CancellationToken ct);

    /// <summary>
    /// What the stock of each warehouse and product is worth by its layers (what is left of each layer times its cost; goods without a layer at the
    /// product's average cost); null when the company costs by the weighted average.
    /// </summary>
    Task<Dictionary<(int WarehouseId, int ProductId), decimal>?> GetStockValuesAsync(int companyId, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}

public record StoreCostLayerRow(
    int Id, int WarehouseId, string WarehouseName, int ProductId, string ProductName, DateTime ReceivedAtUtc,
    decimal OriginalQuantity, decimal RemainingQuantity, decimal UnitCost, string? DocumentType, string? DocumentNo, bool IsOpeningBalance);
