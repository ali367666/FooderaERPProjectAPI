namespace Application.StockPurchase.Dtos.Request;

public class PayStockPurchaseRequest
{
    /// <summary>Amount paid to the supplier now, in AZN. Cannot exceed what is still owed.</summary>
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}
