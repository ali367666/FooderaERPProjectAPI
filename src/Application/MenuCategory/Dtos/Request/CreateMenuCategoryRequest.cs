namespace Application.MenuCategories.Dtos;

public class CreateMenuCategoryRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? ParentCategoryId { get; set; }
}