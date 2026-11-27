namespace Application.Reports.Dtos;

public class ReportCounterpartyLineDto
{
    public int CounterpartyId { get; set; }
    public string Name { get; set; } = default!;
    public decimal CreditSales { get; set; }
    public decimal DebtAdded { get; set; }
    public decimal Paid { get; set; }
    public decimal CurrentDebt { get; set; }
}
