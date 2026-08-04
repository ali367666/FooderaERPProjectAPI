namespace Application.Discounts.Dtos;

public class DiscountResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;          // "Percentage" | "FixedAmount"
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? StartTime { get; set; }   // "HH:mm"
    public string? EndTime { get; set; }
    public int? MaxUsageCount { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentlyValid { get; set; } // computed: active + within date/time/usage window
}
