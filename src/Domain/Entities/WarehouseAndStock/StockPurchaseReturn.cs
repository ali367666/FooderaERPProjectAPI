using Domain.Common;

namespace Domain.Entities.WarehouseAndStock;

/// <summary>
/// Goods handed back to the supplier from a purchase. They leave the warehouse and the purchase's
/// cost and debt shrink by their value.
/// </summary>
public class StockPurchaseReturn : CompanyEntity<int>
{
    public int StockPurchaseId { get; set; }
    public StockPurchase StockPurchase { get; set; } = default!;

    public string? Note { get; set; }

    public ICollection<StockPurchaseReturnLine> Lines { get; set; } = new List<StockPurchaseReturnLine>();
}

public class StockPurchaseReturnLine : BaseEntity<int>
{
    public int StockPurchaseReturnId { get; set; }
    public StockPurchaseReturn StockPurchaseReturn { get; set; } = default!;

    /// <summary>The purchase line the goods are returned from.</summary>
    public int StockPurchaseLineId { get; set; }
    public StockPurchaseLine StockPurchaseLine { get; set; } = default!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = default!;

    public decimal Quantity { get; set; }

    /// <summary>The purchase price per unit (AZN) the credit is worked out at.</summary>
    public decimal UnitPriceAzn { get; set; }
}
