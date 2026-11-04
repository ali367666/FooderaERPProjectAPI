using Domain.Enums;

namespace Application.MenuItems.Dtos;

public class SetComponentInput
{
    public int ComponentMenuItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public int? Limit { get; set; }

    /// <summary>The most the waiter may pick from this component's category in one set; the same value for all components of the category.</summary>
    public int? CategoryMaxPick { get; set; }
}
