namespace Application.Orders.Dtos;

public class OrderResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public int RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public int TableId { get; set; }
    public string? TableName { get; set; }
    public int WaiterId { get; set; }
    public string? WaiterName { get; set; }
    public int? ProcessedByUserId { get; set; }
    public string? ProcessedByUserName { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string Status { get; set; } = default!;
    public string? Note { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? ReceiptNumber { get; set; }

    public List<OrderLineResponse> Lines { get; set; } = new();
}