using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class StockMovement : CompanyEntity<int>
{
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = default!;

    public StockMovementType Type { get; set; }

    // Out mənfi, In müsbət yaza bilərsən, ya da ayrı sütunlarla
    public decimal Quantity { get; set; }

    public int? WarehouseTransferId { get; set; }
    public WarehouseTransfer? WarehouseTransfer { get; set; }

    public string? Note { get; set; }
}