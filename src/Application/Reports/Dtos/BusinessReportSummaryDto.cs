namespace Application.Reports.Dtos;

public class BusinessReportSummaryDto
{
    public int CheckCount { get; set; }

    public int CancelledCount { get; set; }
    public decimal CancelledAmount { get; set; }

    public int OpenCheckCount { get; set; }
    public decimal OpenCheckAmount { get; set; }

    public int GiftCount { get; set; }
    public decimal GiftAmount { get; set; }

    public decimal Expenses { get; set; }

    public decimal CounterpartyCharges { get; set; }

    public decimal CurrentIncomeCash { get; set; }

    public decimal CurrentIncomeBank { get; set; }
}
