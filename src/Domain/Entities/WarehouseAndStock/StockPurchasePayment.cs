using Domain.Common;

namespace Domain.Entities.WarehouseAndStock;

/// <summary>One payment made to the supplier against a purchase: when (CreatedAtUtc), by whom (CreatedByUserId), how much.</summary>
public class StockPurchasePayment : CompanyEntity<int>
{
    public int StockPurchaseId { get; set; }
    public StockPurchase StockPurchase { get; set; } = default!;

    public decimal Amount { get; set; }
    public string? Note { get; set; }

    /// <summary>What was still owed on the purchase right after this payment (AZN).</summary>
    public decimal RemainingAfterAzn { get; set; }
}
