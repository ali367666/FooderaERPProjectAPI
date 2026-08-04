namespace Application.StockPurchase.Dtos.Response;

public class StockPurchaseLineResponse
{
    public int Id { get; set; }
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPriceForeign { get; set; }
    public decimal UnitPriceAzn { get; set; }
    public decimal TotalForeign { get; set; }
    public decimal TotalAzn { get; set; }
}
