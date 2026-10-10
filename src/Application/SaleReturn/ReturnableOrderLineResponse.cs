namespace Application.SaleReturn;

public class ReturnableOrderLineResponse
{
    public int OrderLineId { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = default!;
    public bool IsWeightBased { get; set; }
    public int Quantity { get; set; }
    public int ReturnedQuantity { get; set; }
    public int ReturnableQuantity { get; set; }
    public decimal LineTotal { get; set; }
    /// <summary>Refund per unit of Quantity, after line and order-level discounts.</summary>
    public decimal RefundUnitAmount { get; set; }
}
