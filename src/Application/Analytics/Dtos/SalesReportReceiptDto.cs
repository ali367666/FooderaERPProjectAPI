namespace Application.Analytics.Dtos;

/// <summary>One paid order (one customer sitting) — used by the tables and receipts reports.</summary>
public class SalesReportReceiptDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? ReceiptNumber { get; set; }
    public int TableId { get; set; }
    public string TableName { get; set; } = default!;
    public string WaiterName { get; set; } = default!;
    public int? GuestCount { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ServiceCharge { get; set; }
}
