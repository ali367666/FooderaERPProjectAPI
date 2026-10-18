namespace Application.Reports.Dtos;

public class ReportExpenseLineDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string? CreatedBy { get; set; }
}
