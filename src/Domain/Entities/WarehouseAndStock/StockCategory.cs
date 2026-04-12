using Domain.Common;

namespace Domain.Entities.WarehouseAndStock;

public class StockCategory : BaseEntity<int>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public int CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    public int? ParentId { get; set; }
    public StockCategory? Parent { get; set; }

    public ICollection<StockCategory> Children { get; set; } = new List<StockCategory>();
}