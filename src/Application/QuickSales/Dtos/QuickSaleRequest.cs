namespace Application.QuickSales.Dtos;

public class QuickSaleRequest
{
    public int RestaurantId { get; set; }
    public int WaiterId { get; set; }
    public string? Note { get; set; }

    public List<QuickSaleItemRequest> Items { get; set; } = new();

    public string PaymentMethod { get; set; } = default!;

    public decimal PaidAmount { get; set; }

    public int? BankId { get; set; }

    public bool IsFiscal { get; set; }
}

public class QuickSaleItemRequest
{
    public int MenuItemId { get; set; }

    public int Quantity { get; set; }
}
