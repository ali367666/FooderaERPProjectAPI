using Domain.Common;

namespace Domain.Entities;

public class MenuItem : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public decimal Price { get; set; }
    public string? Portion { get; set; }

    public bool IsActive { get; set; } = true;

    public int MenuCategoryId { get; set; }
    public MenuCategory MenuCategory { get; set; } = default!;

    public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
}