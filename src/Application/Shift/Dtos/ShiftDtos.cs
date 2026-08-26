namespace Application.Shift.Dtos;

public class ShiftResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public int OpenedByUserId { get; set; }
    public string? OpenedByUserName { get; set; }
    public DateTime OpenedAt { get; set; }
    public int? ClosedByUserId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCashAmount { get; set; }
    public decimal? ClosingCashAmount { get; set; }
    public bool IsOpen { get; set; }
}

public class OpenShiftRequest
{
    public int RestaurantId { get; set; }
    public decimal OpeningCashAmount { get; set; }
}

public class CloseShiftRequest
{
    public decimal ClosingCashAmount { get; set; }
}

public class ZReportPaymentBreakdown
{
    public string PaymentMethod { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
}

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
}
