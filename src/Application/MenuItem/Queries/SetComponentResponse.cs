using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.MenuItems.Queries.GetSetComponents;

public class SetComponentResponse
{
    public int ComponentMenuItemId { get; set; }
    public string ComponentMenuItemName { get; set; } = default!;
    public int Quantity { get; set; }
    public int? Limit { get; set; }
}
