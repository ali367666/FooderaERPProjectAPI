using Domain.Common;
using Domain.Entities.WarehouseAndStock;

namespace Domain.Entities.StoreMode;

/// <summary>
/// One delivery of a product into a warehouse, kept while the company costs its store goods by FIFO or LIFO. Goods that leave use up the
/// layers (oldest first for FIFO, newest first for LIFO) and the cost of the layers used is the cost of those goods.
/// </summary>
public class StoreCostLayer : CompanyEntity<int>
{
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public int StoreProductId { get; set; }
    public StoreProduct StoreProduct { get; set; } = default!;

    /// <summary>When the goods came in; the order the layers are used up in.</summary>
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

    public decimal OriginalQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }

    /// <summary>What one unit of this delivery cost (AZN).</summary>
    public decimal UnitCost { get; set; }

    /// <summary>The document the goods came with (purchase, transfer, return...).</summary>
    public string? DocumentType { get; set; }
    public string? DocumentNo { get; set; }

    /// <summary>True for the layer made from the stock that was already there when FIFO / LIFO was switched on.</summary>
    public bool IsOpeningBalance { get; set; }
}
