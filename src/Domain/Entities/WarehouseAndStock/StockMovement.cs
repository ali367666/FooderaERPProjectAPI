using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Entities.WarehouseAndStock;

public class StockMovement : CompanyEntity<int>
{
    /// <summary>Primary warehouse this movement applies to (from-warehouse for out, to-warehouse for in).</summary>
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public int? FromWarehouseId { get; set; }
    public Warehouse? FromWarehouse { get; set; }

    public int? ToWarehouseId { get; set; }
    public Warehouse? ToWarehouse { get; set; }

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = default!;

    public StockMovementType Type { get; set; }

    public StockMovementSourceType SourceType { get; set; }

    /// <summary>Optional link to source entity (e.g. transfer id or stock entry document id).</summary>
    public int? SourceId { get; set; }

    public string? SourceDocumentNo { get; set; }

    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    public decimal Quantity { get; set; }

    public int? WarehouseTransferId { get; set; }
    public WarehouseTransfer? WarehouseTransfer { get; set; }

    public string? Note { get; set; }
}
