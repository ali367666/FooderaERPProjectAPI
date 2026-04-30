using Domain.Common;

namespace Domain.Entities.WarehouseAndStock;

public class WarehouseStockLine : CompanyEntity<int>
{
    public int WarehouseStockDocumentId { get; set; }
    public WarehouseStockDocument WarehouseStockDocument { get; set; } = default!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = default!;

    public decimal Quantity { get; set; }

    /// <summary>Unit identifier (maps to <see cref="Domain.Enums.UnitOfMeasure"/> values until a Unit master table exists).</summary>
    public int UnitId { get; set; }
}
