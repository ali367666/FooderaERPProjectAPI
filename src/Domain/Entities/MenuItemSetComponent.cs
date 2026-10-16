using Domain.Common;

namespace Domain.Entities;

public class MenuItemSetComponent : BaseEntity<int>
{
    public int SetMenuItemId { get; set; }
    public MenuItem SetMenuItem { get; set; } = default!;

    public int ComponentMenuItemId { get; set; }
    public MenuItem ComponentMenuItem { get; set; } = default!;

    public int Quantity { get; set; } = 1;

    /// <summary>Most of this component one SET may contain (per 1 set) — null means no cap.
    /// In the POS the component line can be changed up to Limit × set quantity.</summary>
    public int? Limit { get; set; }
}
