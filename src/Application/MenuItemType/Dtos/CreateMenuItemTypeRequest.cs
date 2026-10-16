namespace Application.MenuItemType.Dtos;

public class CreateMenuItemTypeRequest
{
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
