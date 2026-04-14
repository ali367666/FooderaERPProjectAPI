using Domain.Enums;

namespace Application.MenuItems.Dtos;

public class MenuItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Portion { get; set; }
    public bool IsActive { get; set; }

    public int MenuCategoryId { get; set; }
    public string MenuCategoryName { get; set; } = default!;
    public PreparationType PreparationType { get; set; }
}