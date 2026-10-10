namespace Application.SaleReturn;

public class CreateSaleReturnRequest
{
    public int OrderId { get; set; }
    public string? Reason { get; set; }
    public bool RestockItems { get; set; } = true;
    public List<CreateSaleReturnLineRequest> Lines { get; set; } = new();
}
