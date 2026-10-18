namespace Application.Reports.Dtos;

public class ReportOpenCheckDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public int TableId { get; set; }
    public string TableName { get; set; } = default!;
    public string WaiterName { get; set; } = default!;
    public int? GuestCount { get; set; }
    public DateTime OpenedAt { get; set; }
    public decimal Amount { get; set; }
}
