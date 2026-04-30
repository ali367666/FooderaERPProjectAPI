using Domain.Common;
using Domain.Entities;

namespace Domain.Entities.WarehouseAndStock;

/// <summary>Current on-hand quantity per warehouse and stock item (one row per company + warehouse + item).</summary>
public class WarehouseStock : CompanyEntity<int>
{
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = default!;

    public decimal Quantity { get; set; }

    /// <summary>Unit identifier (maps to <see cref="Domain.Enums.UnitOfMeasure"/> values).</summary>
    public int UnitId { get; set; }
}
