namespace Application.Orders.Dtos;

public class OrderLineResponse
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = default!;
}