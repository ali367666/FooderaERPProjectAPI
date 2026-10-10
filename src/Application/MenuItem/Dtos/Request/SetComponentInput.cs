using Domain.Enums;

namespace Application.MenuItems.Dtos;

public class SetComponentInput
{
    public int ComponentMenuItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public int? Limit { get; set; }
}
