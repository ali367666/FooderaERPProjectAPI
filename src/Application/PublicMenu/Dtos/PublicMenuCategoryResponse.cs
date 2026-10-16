namespace Application.PublicMenu.Dtos;

public class PublicMenuCategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public List<PublicMenuItemResponse> Items { get; set; } = new();
}
