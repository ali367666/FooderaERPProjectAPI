namespace Application.MenuItemType.Dtos;

public class UpdateMenuItemTypeRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
