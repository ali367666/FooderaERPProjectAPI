namespace Application.Analytics.Dtos;

public class SalesReportCancellationDto
{
    public int OrderId { get; set; }
    public DateTime CancelledAt { get; set; }
    public string TableName { get; set; } = default!;
    public string OrderNumber { get; set; } = default!;
    public bool IsWholeOrder { get; set; }
    /// <summary>Removed product, or null when the whole order was cancelled.</summary>
    public string? MenuItemName { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = default!;
    public string? Note { get; set; }
    public bool BeforeKitchen { get; set; }
    public string CancelledBy { get; set; } = default!;
}
