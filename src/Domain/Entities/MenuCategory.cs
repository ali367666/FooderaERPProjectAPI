using Domain.Common;

namespace Domain.Entities;

public class MenuCategory : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}