using Domain.Common;

namespace Domain.Entities;

public class MenuCategory : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int? PrinterId { get; set; }
    public Printer? Printer { get; set; }

    public int? ParentCategoryId { get; set; }
    public MenuCategory? ParentCategory { get; set; }
    public ICollection<MenuCategory> SubCategories { get; set; } = new List<MenuCategory>();

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}