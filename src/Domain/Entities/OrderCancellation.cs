using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Ləğv jurnalı — one cancelled order or one product removed from an order. A snapshot: the
/// removed line (and even a deleted receipt) no longer exists, so names and amounts are copied
/// here at the moment of cancellation and nothing references the order with a foreign key.
/// CreatedByUserId / CreatedAtUtc record who cancelled it and when.
/// </summary>
public class OrderCancellation : CompanyEntity<int>
{
    public int RestaurantId { get; set; }

    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? ReceiptNumber { get; set; }

    public int TableId { get; set; }
    public string TableName { get; set; } = default!;

    /// <summary>True when the whole order was cancelled / deleted; false for a single removed product.</summary>
    public bool IsWholeOrder { get; set; }

    public int? MenuItemId { get; set; }
    /// <summary>Removed product's name, or null for a whole-order cancellation.</summary>
    public string? MenuItemName { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Chosen reason (from the preset list) — required.</summary>
    public string Reason { get; set; } = default!;
    public string? Note { get; set; }

    /// <summary>The product was removed before it was sent to the kitchen (no reason was asked).</summary>
    public bool BeforeKitchen { get; set; }

    public string CancelledByName { get; set; } = default!;
}
