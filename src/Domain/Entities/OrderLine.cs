using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class OrderLine : CompanyEntity<int>
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = default!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public PreparationType PreparationType { get; set; }
    public string? Note { get; set; }
    public bool IsStockDeducted { get; set; }

    public OrderLineStatus Status { get; set; } = OrderLineStatus.Pending;
}