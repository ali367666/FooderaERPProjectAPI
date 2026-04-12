using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;
public class Order : CompanyEntity<int>
{
    public string OrderNumber { get; set; } = default!;

    public int RestaurantId { get; set; }   // ✅ əlavə et
    public Restaurant Restaurant { get; set; } = default!;

    public int TableId { get; set; }
    public RestaurantTable Table { get; set; } = default!;

    public int WaiterId { get; set; }
    public Employee Waiter { get; set; } = default!;

    public OrderStatus Status { get; set; } = OrderStatus.Open;

    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
}