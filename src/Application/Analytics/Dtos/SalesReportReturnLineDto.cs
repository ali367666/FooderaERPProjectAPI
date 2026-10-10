namespace Application.Analytics.Dtos;

public class SalesReportReturnLineDto
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
}
