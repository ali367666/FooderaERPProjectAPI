namespace Application.Shift.Dtos;

public class ZReportPaymentBreakdown
{
    public string PaymentMethod { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
}
