using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.WarehouseAndStock;

public class StockItem : BaseEntity<int>
{
    public string Name { get; set; } = default!;
    public string? Barcode { get; set; }
    public StockItemType Type { get; set; }
    public UnitOfMeasure Unit { get; set; }

    public int CategoryId { get; set; }
    public StockCategory Category { get; set; } = default!;

    public int CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    public ICollection<WarehouseStock> WarehouseStocks { get; set; } = new List<WarehouseStock>();
}
