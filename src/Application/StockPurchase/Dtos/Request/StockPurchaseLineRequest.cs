namespace Application.StockPurchase.Dtos.Request;

public class StockPurchaseLineRequest
{
    public int? Id { get; set; }
    public int StockItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPriceForeign { get; set; }
}
