using Domain.Common;

namespace Domain.Entities;

public class ScaleDevice : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string? Brand { get; set; }
    public string? ConnectionInfo { get; set; }
    public bool IsActive { get; set; } = true;
}
