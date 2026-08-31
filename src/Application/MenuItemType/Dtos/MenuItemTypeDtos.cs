namespace Application.MenuItemType.Dtos;

public class MenuItemTypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class CreateMenuItemTypeRequest
{
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

public class UpdateMenuItemTypeRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
