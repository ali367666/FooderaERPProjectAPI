using Domain.Enums;

namespace Application.MenuItems.Dtos;

public class UpdateMenuItemRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Portion { get; set; }
    public int MenuCategoryId { get; set; }
    public PreparationType PreparationType { get; set; }
    public bool IsActive { get; set; }
}