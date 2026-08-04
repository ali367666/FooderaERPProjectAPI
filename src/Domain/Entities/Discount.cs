using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Discount : CompanyEntity<int>
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    public DiscountType Type { get; set; }
    public decimal Value { get; set; }              // percentage (0-100) or fixed AZN amount

    public decimal? MinOrderAmount { get; set; }     // order subtotal must be >= this to apply
    public decimal? MaxDiscountAmount { get; set; }  // cap for percentage discounts

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>Optional daily time window, e.g. happy hour 17:00-19:00. Null = all day.</summary>
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    public int? MaxUsageCount { get; set; }          // null = unlimited
    public int UsedCount { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}
