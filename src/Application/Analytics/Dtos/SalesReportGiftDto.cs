namespace Application.Analytics.Dtos;

public class SalesReportGiftDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? ReceiptNumber { get; set; }
    public string TableName { get; set; } = default!;
    public string MenuItemName { get; set; } = default!;
    public int Quantity { get; set; }
    /// <summary>Menu value of the gifted items (unit price × quantity) — what the restaurant gave away.</summary>
    public decimal Value { get; set; }
    /// <summary>When it was marked as a gift; null for gifts made before this was tracked.</summary>
    public DateTime? GiftedAt { get; set; }
    public string GiftedBy { get; set; } = default!;
}
