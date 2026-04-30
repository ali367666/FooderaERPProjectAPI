namespace Application.Orders.Dtos;

public class OrderReceiptResponse
{
    public string ReceiptNumber { get; set; } = default!;
    public string OrderNumber { get; set; } = default!;
    public string RestaurantName { get; set; } = default!;
    public string TableName { get; set; } = default!;
    public string WaiterName { get; set; } = default!;
    public DateTime OpenedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string PaymentMethod { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public List<OrderReceiptLineResponse> Lines { get; set; } = new();
}

public class OrderReceiptLineResponse
{
    public string MenuItemName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
