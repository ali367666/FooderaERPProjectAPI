using Domain.Enums;

namespace Application.StockWriteOffs;

/// <summary>Takes goods out of a warehouse as waste, spoiled or broken (e.g. 10 of the 20 kg of meat delivered).</summary>
public class CreateStockWriteOffRequest
{
    public int WarehouseId { get; set; }
    public int StockItemId { get; set; }
    public decimal Quantity { get; set; }
    public StockWriteOffReason Reason { get; set; } = StockWriteOffReason.Waste;
    public string? Note { get; set; }

    /// <summary>Optional: the purchase the goods came from. Its debt stays as it is.</summary>
    public int? StockPurchaseId { get; set; }
}

public class StockWriteOffResponse
{
    public int Id { get; set; }
    public DateTime WrittenOffAtUtc { get; set; }
    public int? WrittenOffByUserId { get; set; }
    public string? WrittenOffByUserName { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitCostAzn { get; set; }

    /// <summary>Quantity times unit cost: what the loss is worth (AZN).</summary>
    public decimal LossAzn { get; set; }

    public StockWriteOffReason Reason { get; set; }
    public string ReasonName { get; set; } = default!;
    public string? Note { get; set; }
    public int? StockPurchaseId { get; set; }
    public string? PurchaseDocumentNo { get; set; }
}

public class StockWriteOffListResponse
{
    public decimal TotalLossAzn { get; set; }
    public List<StockWriteOffResponse> Items { get; set; } = new();
}
