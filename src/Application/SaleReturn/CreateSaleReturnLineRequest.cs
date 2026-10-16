namespace Application.SaleReturn;

public class CreateSaleReturnLineRequest
{
    public int OrderLineId { get; set; }
    public int Quantity { get; set; }
}
