namespace Application.QuickSales.Dtos;

/// <summary>
/// "Tez satış": the whole sale in one go — the products and how the customer pays. There is no
/// open order to build up line by line, so the server needs a single round trip.
/// </summary>
public class QuickSaleRequest
{
    public int RestaurantId { get; set; }
    public int WaiterId { get; set; }
    public string? Note { get; set; }

    public List<QuickSaleItemRequest> Items { get; set; } = new();

    /// <summary>Cash or Card.</summary>
    public string PaymentMethod { get; set; } = default!;

    /// <summary>Cash: the money handed over. Card: ignored, the server settles exactly what is due.</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>The bank a card payment goes through. Required for Card.</summary>
    public int? BankId { get; set; }

    /// <summary>Rung through the fiscal (tax) register.</summary>
    public bool IsFiscal { get; set; }
}

public class QuickSaleItemRequest
{
    public int MenuItemId { get; set; }

    /// <summary>Pieces, or grams for a weight-sold product (same as an order line).</summary>
    public int Quantity { get; set; }
}
