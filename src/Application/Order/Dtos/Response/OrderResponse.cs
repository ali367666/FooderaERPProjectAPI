namespace Application.Orders.Dtos;

public class OrderResponse
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public int RestaurantId { get; set; }
    public int TableId { get; set; }
    public string? TableName { get; set; }
    public int WaiterId { get; set; }
    public string? WaiterName { get; set; }
    public string Status { get; set; } = default!;
    public string? Note { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal TotalAmount { get; set; }

    public List<OrderLineResponse> Lines { get; set; } = new();
}