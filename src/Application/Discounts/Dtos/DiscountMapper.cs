using Domain.Entities;

namespace Application.Discounts.Dtos;

public static class DiscountMapper
{
    public static DiscountResponse ToResponse(Discount d)
    {
        var now = DateTime.UtcNow;
        var withinDate = now.Date >= d.StartDate.Date && now.Date <= d.EndDate.Date;
        var withinTime = true;
        if (d.StartTime.HasValue && d.EndTime.HasValue)
        {
            var nowTime = now.TimeOfDay;
            withinTime = d.StartTime.Value <= d.EndTime.Value
                ? nowTime >= d.StartTime.Value && nowTime <= d.EndTime.Value
                : nowTime >= d.StartTime.Value || nowTime <= d.EndTime.Value; // overnight window
        }
        var withinUsage = d.MaxUsageCount is null || d.UsedCount < d.MaxUsageCount.Value;

        return new DiscountResponse
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name,
            Type = d.Type.ToString(),
            Value = d.Value,
            MinOrderAmount = d.MinOrderAmount,
            MaxDiscountAmount = d.MaxDiscountAmount,
            StartDate = d.StartDate,
            EndDate = d.EndDate,
            StartTime = d.StartTime?.ToString(@"hh\:mm"),
            EndTime = d.EndTime?.ToString(@"hh\:mm"),
            MaxUsageCount = d.MaxUsageCount,
            UsedCount = d.UsedCount,
            IsActive = d.IsActive,
            IsCurrentlyValid = d.IsActive && withinDate && withinTime && withinUsage,
        };
    }
}
