namespace Application.Analytics.Dtos;

public class SalesReportProductLineDto
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
    /// <summary>Share of all product revenue in the period, 0–100.</summary>
    public decimal Percent { get; set; }
}
