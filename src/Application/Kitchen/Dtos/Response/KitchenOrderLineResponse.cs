using Domain.Enums;

namespace Application.Kitchen.Dtos;

public class KitchenOrderLineResponse
{
    public int OrderLineId { get; set; }
    public OrderLineStatus OrderLineStatus { get; set; }
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public int TableId { get; set; }
    public string? TableName { get; set; }
    public string? RestaurantName { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = default!;
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public OrderLineStatus KitchenStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}