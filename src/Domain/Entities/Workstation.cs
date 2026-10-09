using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// A named POS monitor registered by the company admin. The workstation setup screen picks one of
/// these, so the screens of one company can be told apart (e.g. "Kassa 1", "Bar").
/// </summary>
public class Workstation : CompanyEntity<int>
{
    public int? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenAtUtc { get; set; }
}
