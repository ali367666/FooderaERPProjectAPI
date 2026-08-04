namespace Application.Discounts.Dtos;

public class CreateDiscountRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;     // "Percentage" | "FixedAmount"
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? StartTime { get; set; }            // "HH:mm"
    public string? EndTime { get; set; }
    public int? MaxUsageCount { get; set; }
}
