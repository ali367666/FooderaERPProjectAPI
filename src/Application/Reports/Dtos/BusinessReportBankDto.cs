namespace Application.Reports.Dtos;

/// <summary>Card income of the period through one payment bank.</summary>
public class BusinessReportBankDto
{
    public string BankName { get; set; } = default!;

    public decimal Amount { get; set; }
}
