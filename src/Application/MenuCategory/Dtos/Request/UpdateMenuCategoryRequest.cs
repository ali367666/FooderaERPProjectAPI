namespace Application.MenuCategories.Dtos;

public class UpdateMenuCategoryRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int? ParentCategoryId { get; set; }
}