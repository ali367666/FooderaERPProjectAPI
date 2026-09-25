using Domain.Common;

namespace Domain.Entities;

public class SaleReturnLine : BaseEntity<int>
{
    public int SaleReturnId { get; set; }
    public SaleReturn SaleReturn { get; set; } = default!;

    public int OrderLineId { get; set; }
    public OrderLine OrderLine { get; set; } = default!;

    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = default!;

    /// <summary>Same unit as OrderLine.Quantity (grams for weight-sold items).</summary>
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
}
