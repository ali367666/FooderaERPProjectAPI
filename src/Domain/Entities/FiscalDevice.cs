using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class FiscalDevice : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public string Name { get; set; } = default!;
    public FiscalDeviceProvider Provider { get; set; } = FiscalDeviceProvider.Other;
    public string? ConnectionInfo { get; set; }
    public bool IsActive { get; set; } = true;
}
