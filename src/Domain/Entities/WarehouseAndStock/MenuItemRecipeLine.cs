using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Entities.WarehouseAndStock;

public class MenuItemRecipeLine : CompanyEntity<int>
{
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = default!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = default!;

    public decimal QuantityPerPortion { get; set; }
    public UnitOfMeasure Unit { get; set; }
}
