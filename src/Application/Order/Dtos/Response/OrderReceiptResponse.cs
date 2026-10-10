namespace Application.Orders.Dtos;

public class OrderReceiptResponse
{
    public string ReceiptNumber { get; set; } = default!;
    public string OrderNumber { get; set; } = default!;
    public string RestaurantName { get; set; } = default!;
    public string? RestaurantAddress { get; set; }
    public string TableName { get; set; } = default!;
    /// <summary>The hall / section the table is in (zal), if any.</summary>
    public string? SectionName { get; set; }
    public string WaiterName { get; set; } = default!;
    public DateTime OpenedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    /// <summary>When the guests left — payment time, else the order's close time.</summary>
    public DateTime? ClosedAt { get; set; }
    public string PaymentMethod { get; set; } = default!;
    /// <summary>Sum of the product lines (gifts count 0) — before discount and service charge.</summary>
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal TableRentalAmount { get; set; }
    /// <summary>What the customer pays: lines − discount + service charge + table rental.</summary>
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal VatAmount { get; set; }

    /// <summary>Rung through the fiscal register — only then does the receipt carry VAT.</summary>
    public bool IsFiscal { get; set; }

    /// <summary>How the bill was settled — shown under the grand total. All 0 for an unpaid pre-check.</summary>
    public decimal CashPaidAmount { get; set; }
    public decimal CardPaidAmount { get; set; }
    public decimal CreditPaidAmount { get; set; }

    public List<OrderReceiptLineResponse> Lines { get; set; } = new();
}

public class OrderReceiptLineResponse
{
    public string MenuItemName { get; set; } = default!;
    public int MenuCategoryId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal VatAmount { get; set; }
    /// <summary>Complimentary item — its name already carries the "(Hədiyyə)" mark for printing.</summary>
    public bool IsGift { get; set; }

    /// <summary>
    /// On a part-payment receipt: this item was already paid by an earlier guest. It is listed (so the
    /// receipt tells the whole story) but its amount is not part of this receipt's total.
    /// </summary>
    public bool PaidEarlier { get; set; }
}
