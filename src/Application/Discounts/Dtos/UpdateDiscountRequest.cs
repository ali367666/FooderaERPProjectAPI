namespace Application.Discounts.Dtos;

public class UpdateDiscountRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int? MaxUsageCount { get; set; }
    public bool IsActive { get; set; }
}
