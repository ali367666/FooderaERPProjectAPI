using Domain.Common;

namespace Domain.Entities;

public class WarehouseStock : CompanyEntity<int>
{
    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = default!;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public decimal QuantityOnHand { get; set; }
    public decimal? MinLevel { get; set; }
}
