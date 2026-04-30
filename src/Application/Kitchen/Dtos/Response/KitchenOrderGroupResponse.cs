using Domain.Enums;

namespace Application.Kitchen.Dtos;

public class KitchenOrderGroupResponse
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public int TableId { get; set; }
    public string TableName { get; set; } = default!;
    public OrderStatus OrderStatus { get; set; }
    public DateTime OpenedAt { get; set; }
    public List<KitchenOrderLineResponse> Lines { get; set; } = new();
}
