namespace Application.SaleReturn;

public class SaleReturnLineResponse
{
    public int OrderLineId { get; set; }
    public string MenuItemName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
}
