using Domain.Common;

namespace Domain.Entities;

public class MenuItemSetComponent : BaseEntity<int>
{
    public int SetMenuItemId { get; set; }
    public MenuItem SetMenuItem { get; set; } = default!;

    public int ComponentMenuItemId { get; set; }
    public MenuItem ComponentMenuItem { get; set; } = default!;

    public int Quantity { get; set; } = 1;
}
