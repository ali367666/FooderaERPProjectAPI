namespace Application.Analytics.Dtos;

/// <summary>Card income of the period through one payment bank (Kapital, Paşa, ABB, QR kod).</summary>
public class SalesReportBankDto
{
    public string BankName { get; set; } = default!;
    public int PaymentCount { get; set; }
    public decimal TotalAmount { get; set; }
}
