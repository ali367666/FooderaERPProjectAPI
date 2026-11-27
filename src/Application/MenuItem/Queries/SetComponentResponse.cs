using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.MenuItems.Queries.GetSetComponents;

public class SetComponentResponse
{
    public int ComponentMenuItemId { get; set; }
    public string ComponentMenuItemName { get; set; } = default!;
    public int Quantity { get; set; }
    public int? Limit { get; set; }
    public int? ComponentCategoryId { get; set; }

    /// <summary>The most the waiter may pick in total from this component's category in one set (null = no cap).</summary>
    public int? CategoryMaxPick { get; set; }
}
