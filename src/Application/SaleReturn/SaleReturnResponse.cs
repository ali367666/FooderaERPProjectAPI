namespace Application.SaleReturn;

public class SaleReturnResponse
{
    public int Id { get; set; }
    public string ReturnNumber { get; set; } = default!;
    public int OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string PaymentMethod { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public bool RestockItems { get; set; }
    public string? Reason { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<SaleReturnLineResponse> Lines { get; set; } = new();
}
