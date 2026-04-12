namespace Application.Orders.Dtos;

public class AddOrderLineRequest
{
    public int OrderId { get; set; }
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}