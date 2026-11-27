using Domain.Common;

namespace Domain.Entities;

public class MenuCategory : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Sets category: its products are SETs whose contents the waiter picks.</summary>
    public bool IsSetCategory { get; set; }

    /// <summary>When true no product of this category (nor of its sub-categories) takes part in discounts.</summary>
    public bool ExcludeFromDiscount { get; set; }

    public int? ParentCategoryId { get; set; }
    public MenuCategory? ParentCategory { get; set; }
    public ICollection<MenuCategory> SubCategories { get; set; } = new List<MenuCategory>();

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}