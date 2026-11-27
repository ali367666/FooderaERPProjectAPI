namespace Application.Shift.Dtos;

/// <summary>Card income of the shift by the bank it went through.</summary>
public class ZReportBankBreakdown
{
    public string BankName { get; set; } = default!;
    public int PaymentCount { get; set; }
    public decimal TotalAmount { get; set; }
}
