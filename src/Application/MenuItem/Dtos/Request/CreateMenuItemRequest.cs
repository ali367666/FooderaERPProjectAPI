namespace Application.MenuItems.Dtos;

public class CreateMenuItemRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Portion { get; set; }
    public int MenuCategoryId { get; set; }
}