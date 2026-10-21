namespace Application.Reports.Dtos;

public class BusinessReportIncomeDto
{
    public decimal Cash { get; set; }

    public decimal Bank { get; set; }

    /// <summary>The card income (<see cref="Bank"/>) split by payment bank — Kapital, Paşa, ABB, QR kod.</summary>
    public List<BusinessReportBankDto> BankBreakdown { get; set; } = new();

    public decimal Total { get; set; }

    public decimal CashIn { get; set; }

    public decimal CashOut { get; set; }

    public decimal CashReturns { get; set; }

    public decimal CashRemaining { get; set; }
}
