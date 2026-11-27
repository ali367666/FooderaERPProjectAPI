namespace Application.StockPurchase.Dtos.Response;

public class StockPurchaseLineResponse
{
    public int Id { get; set; }
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public decimal Quantity { get; set; }

    /// <summary>How much of <see cref="Quantity"/> has been handed back to the supplier.</summary>
    public decimal ReturnedQuantity { get; set; }

    /// <summary>What can still be returned: <see cref="Quantity"/> minus <see cref="ReturnedQuantity"/>.</summary>
    public decimal ReturnableQuantity { get; set; }

    public decimal UnitPriceForeign { get; set; }
    public decimal UnitPriceAzn { get; set; }
    public decimal TotalForeign { get; set; }
    public decimal TotalAzn { get; set; }
}
