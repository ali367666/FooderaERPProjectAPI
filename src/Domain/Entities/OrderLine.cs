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

    /// <summary>Complimentary item — printed/shown, but contributes 0 to the order total.</summary>
    public bool IsGift { get; set; }

    /// <summary>Manual per-line discount (AZN) applied to this line only.</summary>
    public decimal DiscountAmount { get; set; }
    public bool IsStockDeducted { get; set; }
    public DateTime? HoldUntilUtc { get; set; }
    public DateTime? KitchenPrintedAt { get; set; }
    public DateTime? TimeBasedStartedAt { get; set; }
    public DateTime? TimeBasedStoppedAt { get; set; }

    public OrderLineStatus Status { get; set; } = OrderLineStatus.Pending;

    public int? ParentLineId { get; set; }
    public OrderLine? ParentLine { get; set; }
}