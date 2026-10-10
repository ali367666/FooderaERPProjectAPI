namespace Application.Shift.Dtos;

public class ZReportResponse
{
    public int ShiftId { get; set; }
    public int RestaurantId { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCashAmount { get; set; }
    public decimal? ClosingCashAmount { get; set; }
    public int OrderCount { get; set; }
    public decimal GrossTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalServiceCharge { get; set; }
    public List<ZReportPaymentBreakdown> PaymentBreakdown { get; set; } = new();
    public int ReturnCount { get; set; }
    public decimal TotalReturns { get; set; }
    /// <summary>GrossTotal minus returns made during the shift.</summary>
    public decimal NetTotal { get; set; }
    public List<ZReportPaymentBreakdown> ReturnBreakdown { get; set; } = new();
}
