namespace Application.Order.Dtos.Request;

public class UpdateOrderRequest
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public int TableId { get; set; }
    public int WaiterId { get; set; }
    public string? Note { get; set; }
    public string? Status { get; set; }
}