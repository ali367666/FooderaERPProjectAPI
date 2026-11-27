using Domain.Enums;

namespace Application.StoreMode;

// ---- Inventory (stock count) ---------------------------------------------------------------

public class StartStoreInventoryRequest
{
    public int WarehouseId { get; set; }

    /// <summary>Count only this category; left out, the whole warehouse is counted.</summary>
    public int? CategoryId { get; set; }
    public string? Note { get; set; }
}

public class SaveStoreInventoryCountsRequest
{
    public List<StoreInventoryCountLine> Lines { get; set; } = new();
}

public class StoreInventoryCountLine
{
    public int LineId { get; set; }

    /// <summary>What was counted; null clears the entry (the line is then left unchanged on confirmation).</summary>
    public decimal? CountedQuantity { get; set; }
    public string? Reason { get; set; }
}

public class StoreInventoryLineResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string? Barcode { get; set; }
    public UnitOfMeasure Unit { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal? CountedQuantity { get; set; }

    /// <summary>Counted minus expected (shown live while counting, fixed once confirmed).</summary>
    public decimal Difference { get; set; }
    public decimal UnitCost { get; set; }
    public decimal DifferenceValue { get; set; }
    public string? Reason { get; set; }
}

public class StoreInventoryResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public StoreInventoryStatus Status { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public string? ConfirmedByName { get; set; }
    public int LineCount { get; set; }
    public int CountedCount { get; set; }
    public int DifferenceCount { get; set; }

    /// <summary>Surplus value (plus) and shortage value (minus) of the differences, in AZN.</summary>
    public decimal SurplusValue { get; set; }
    public decimal ShortageValue { get; set; }
    public List<StoreInventoryLineResponse> Lines { get; set; } = new();
}

// ---- Transfers -----------------------------------------------------------------------------

public class SaveStoreTransferRequest
{
    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }
    public string? Note { get; set; }
    public List<SaveStoreTransferLineRequest> Lines { get; set; } = new();
}

public class SaveStoreTransferLineRequest
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>For a serial / IMEI product: the units to send, as many as the quantity.</summary>
    public List<string>? SerialNumbers { get; set; }
}

public class ReceiveStoreTransferRequest
{
    public List<ReceiveStoreTransferLineRequest> Lines { get; set; } = new();

    /// <summary>True: what has not arrived is written down as a shortage and the transfer is closed.</summary>
    public bool CloseWithShortage { get; set; }
}

public class ReceiveStoreTransferLineRequest
{
    public int LineId { get; set; }
    public decimal Quantity { get; set; }
    public List<string>? SerialNumbers { get; set; }
}

public class StoreTransferLineResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public bool TracksSerial { get; set; }
    public decimal Quantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }

    /// <summary>Sent and not yet received.</summary>
    public decimal InTransitQuantity { get; set; }
    public List<string> SerialNumbers { get; set; } = new();
    public List<string> ReceivedSerialNumbers { get; set; } = new();
}

public class StoreTransferResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public int FromWarehouseId { get; set; }
    public string FromWarehouseName { get; set; } = default!;
    public int ToWarehouseId { get; set; }
    public string ToWarehouseName { get; set; } = default!;
    public StoreTransferStatus Status { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? ShippedAtUtc { get; set; }
    public string? ShippedByName { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public string? ReceivedByName { get; set; }
    public int LineCount { get; set; }
    public decimal InTransitValue { get; set; }
    public List<StoreTransferLineResponse> Lines { get; set; } = new();
}
