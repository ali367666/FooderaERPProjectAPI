namespace Application.MenuCategories.Dtos;

public class CreateMenuCategoryRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}