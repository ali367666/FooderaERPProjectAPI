using Domain.Common;

namespace Domain.Entities.WarehouseAndStock;

public class StockPurchaseLine : BaseEntity<int>
{
    public int StockPurchaseId { get; set; }
    public StockPurchase StockPurchase { get; set; } = default!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = default!;

    public decimal Quantity { get; set; }

    // Xarici valyutada vahid qiyməti
    public decimal UnitPriceForeign { get; set; }

    // AZN-ə çevrilmiş vahid qiyməti (UnitPriceForeign * ExchangeRate)
    public decimal UnitPriceAzn { get; set; }
}
