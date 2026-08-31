using Domain.Common;

namespace Domain.Entities;

public class MenuItemType : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
