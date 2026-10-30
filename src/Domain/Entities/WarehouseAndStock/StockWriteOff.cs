using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.WarehouseAndStock;

/// <summary>
/// Goods taken out of a warehouse because they were wasted, spoiled or broken. The stock drops, but a
/// purchase's debt does not: the supplier is still owed for what was delivered. CreatedAtUtc and
/// CreatedByUserId say when and by whom.
/// </summary>
public class StockWriteOff : CompanyEntity<int>
{
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = default!;

    public decimal Quantity { get; set; }

    /// <summary>The item's cost per unit (AZN) when it was written off, so the loss keeps its value later.</summary>
    public decimal UnitCostAzn { get; set; }

    public StockWriteOffReason Reason { get; set; }
    public string? Note { get; set; }

    /// <summary>The purchase the goods came from, when the write-off is about a specific delivery.</summary>
    public int? StockPurchaseId { get; set; }
    public StockPurchase? StockPurchase { get; set; }
}
