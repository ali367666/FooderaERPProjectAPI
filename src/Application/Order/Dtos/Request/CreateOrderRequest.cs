namespace Application.Orders.Dtos;

public class CreateOrderRequest
{
    public int RestaurantId { get; set; }
    public int TableId { get; set; }
    public int WaiterId { get; set; }
    public string? Note { get; set; }
}