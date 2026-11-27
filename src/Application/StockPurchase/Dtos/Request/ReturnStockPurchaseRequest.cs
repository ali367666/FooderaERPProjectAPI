namespace Application.StockPurchase.Dtos.Request;

/// <summary>Hands some of a purchase's goods back to the supplier (e.g. 5 of the 10 colas delivered).</summary>
public class ReturnStockPurchaseRequest
{
    public List<ReturnStockPurchaseLineRequest> Lines { get; set; } = new();
    public string? Note { get; set; }
}

public class ReturnStockPurchaseLineRequest
{
    /// <summary>The purchase line (<c>lines[].id</c> of the purchase) the goods are returned from.</summary>
    public int StockPurchaseLineId { get; set; }

    /// <summary>How much to hand back; at most what is still returnable on that line.</summary>
    public decimal Quantity { get; set; }
}
