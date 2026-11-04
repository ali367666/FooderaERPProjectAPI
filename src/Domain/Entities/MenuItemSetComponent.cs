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

    /// <summary>
    /// Most the waiter may pick in total from this component's menu category inside ONE set (per 1 set), e.g. 3 desserts in the set
    /// but only 1 of them may go on the order. Null means no cap for the category. Every component of a category carries the same value.
    /// </summary>
    public int? CategoryMaxPick { get; set; }
}
