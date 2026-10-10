namespace Application.Analytics.Dtos;

public class SalesReportCategoryLineDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
    /// <summary>Share of all category revenue in the period, 0–100.</summary>
    public decimal Percent { get; set; }
}
