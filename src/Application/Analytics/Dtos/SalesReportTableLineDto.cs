namespace Application.Analytics.Dtos;

public class SalesReportTableLineDto
{
    public int TableId { get; set; }
    public string TableName { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    /// <summary>Each customer sitting at this table in the period, oldest first.</summary>
    public List<SalesReportReceiptDto> Sessions { get; set; } = new();
}
