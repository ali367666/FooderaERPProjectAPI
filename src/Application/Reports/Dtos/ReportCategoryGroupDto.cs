using Application.Analytics.Dtos;

namespace Application.Reports.Dtos;

public class ReportCategoryGroupDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal Percent { get; set; }
    public List<SalesReportProductLineDto> Products { get; set; } = new();
}
