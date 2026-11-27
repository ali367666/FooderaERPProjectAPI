using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.StoreMode;

/// <summary>
/// A stock count of a warehouse (or one category of it). It records what the system expected and what was
/// counted; stock changes only when the count is confirmed, as adjustment movements for the differences.
/// </summary>
public class StoreInventory : CompanyEntity<int>
{
    public string DocumentNo { get; set; } = default!;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    /// <summary>When set, only products of this category are counted.</summary>
    public int? CategoryId { get; set; }
    public StoreCategory? Category { get; set; }

    public StoreInventoryStatus Status { get; set; } = StoreInventoryStatus.Draft;
    public string? Note { get; set; }

    public DateTime? ConfirmedAtUtc { get; set; }
    public int? ConfirmedByUserId { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    public ICollection<StoreInventoryLine> Lines { get; set; } = new List<StoreInventoryLine>();
}

public class StoreInventoryLine : BaseEntity<int>
{
    public int StoreInventoryId { get; set; }
    public StoreInventory StoreInventory { get; set; } = default!;

    public int StoreProductId { get; set; }
    public StoreProduct StoreProduct { get; set; } = default!;

    /// <summary>What the system held when the count was opened.</summary>
    public decimal ExpectedQuantity { get; set; }

    /// <summary>What was counted; null until someone enters it (such a line is left unchanged on confirmation).</summary>
    public decimal? CountedQuantity { get; set; }

    /// <summary>Counted minus expected, worked out when the count is confirmed.</summary>
    public decimal Difference { get; set; }

    /// <summary>Cost per unit (AZN) when confirmed, so the value of the difference keeps its meaning.</summary>
    public decimal UnitCost { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Goods moving from one warehouse to another. Once shipped they leave the source stock and are "in transit"
/// until received; sent and received quantities are kept apart so a shortage shows.
/// </summary>
public class StoreTransfer : CompanyEntity<int>
{
    public string DocumentNo { get; set; } = default!;

    public int FromWarehouseId { get; set; }
    public Warehouse FromWarehouse { get; set; } = default!;
    public int ToWarehouseId { get; set; }
    public Warehouse ToWarehouse { get; set; } = default!;

    public StoreTransferStatus Status { get; set; } = StoreTransferStatus.Draft;
    public string? Note { get; set; }

    public DateTime? ShippedAtUtc { get; set; }
    public int? ShippedByUserId { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public int? ReceivedByUserId { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    public ICollection<StoreTransferLine> Lines { get; set; } = new List<StoreTransferLine>();
}

public class StoreTransferLine : BaseEntity<int>
{
    public int StoreTransferId { get; set; }
    public StoreTransfer StoreTransfer { get; set; } = default!;

    public int StoreProductId { get; set; }
    public StoreProduct StoreProduct { get; set; } = default!;

    /// <summary>What was asked to move.</summary>
    public decimal Quantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }

    /// <summary>What never arrived, noted when the transfer is closed with a shortage.</summary>
    public decimal ShortageQuantity { get; set; }

    /// <summary>IMEI / serial numbers sent (for tracked products), comma separated.</summary>
    public string? SerialNumbers { get; set; }

    /// <summary>Serial numbers already received, comma separated.</summary>
    public string? ReceivedSerialNumbers { get; set; }

    /// <summary>The product's cost per unit when shipped.</summary>
    public decimal UnitCost { get; set; }
}
