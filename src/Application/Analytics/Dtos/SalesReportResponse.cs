namespace Application.Analytics.Dtos;

public class SalesReportResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal CashTotal { get; set; }
    public decimal CardTotal { get; set; }
    public int OrderCount { get; set; }

    public List<SalesReportProductLineDto> Products { get; set; } = new();
    public List<SalesReportWaiterLineDto> Waiters { get; set; } = new();
    public List<SalesReportCategoryLineDto> Categories { get; set; } = new();
    public List<SalesReportTableLineDto> Tables { get; set; } = new();
    /// <summary>Every paid order in the period, newest first.</summary>
    public List<SalesReportReceiptDto> Receipts { get; set; } = new();
    public decimal TotalServiceCharge { get; set; }
    /// <summary>Cancelled orders and removed products in the period, newest first.</summary>
    public List<SalesReportCancellationDto> Cancellations { get; set; } = new();
    /// <summary>Gift (complimentary) products on orders paid in the period, newest first.</summary>
    public List<SalesReportGiftDto> Gifts { get; set; } = new();

    // Geri qaytarmalar — returns made within the period (by return date, like the shift Z report).
    public int ReturnCount { get; set; }
    public decimal TotalReturns { get; set; }
    public decimal CashReturns { get; set; }
    public decimal CardReturns { get; set; }
    public decimal CreditReturns { get; set; }
    /// <summary>TotalRevenue minus returns in the period.</summary>
    public decimal NetRevenue { get; set; }
    public List<SalesReportReturnLineDto> ReturnedProducts { get; set; } = new();
}

public class SalesReportReturnLineDto
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
}

public class SalesReportProductLineDto
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
    /// <summary>Share of all product revenue in the period, 0–100.</summary>
    public decimal Percent { get; set; }
}

public class SalesReportWaiterLineDto
{
    public int WaiterId { get; set; }
    public string WaiterName { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    /// <summary>Service charge collected on this waiter's orders — the amount owed to the waiter.</summary>
    public decimal ServiceCharge { get; set; }
}

public class SalesReportCategoryLineDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
    /// <summary>Share of all category revenue in the period, 0–100.</summary>
    public decimal Percent { get; set; }
}

/// <summary>One paid order (one customer sitting) — used by the tables and receipts reports.</summary>
public class SalesReportReceiptDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? ReceiptNumber { get; set; }
    public int TableId { get; set; }
    public string TableName { get; set; } = default!;
    public string WaiterName { get; set; } = default!;
    public int? GuestCount { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ServiceCharge { get; set; }
}

public class SalesReportCancellationDto
{
    public int OrderId { get; set; }
    public DateTime CancelledAt { get; set; }
    public string TableName { get; set; } = default!;
    public string OrderNumber { get; set; } = default!;
    public bool IsWholeOrder { get; set; }
    /// <summary>Removed product, or null when the whole order was cancelled.</summary>
    public string? MenuItemName { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = default!;
    public string? Note { get; set; }
    public bool BeforeKitchen { get; set; }
    public string CancelledBy { get; set; } = default!;
}

public class SalesReportGiftDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? ReceiptNumber { get; set; }
    public string TableName { get; set; } = default!;
    public string MenuItemName { get; set; } = default!;
    public int Quantity { get; set; }
    /// <summary>Menu value of the gifted items (unit price × quantity) — what the restaurant gave away.</summary>
    public decimal Value { get; set; }
    /// <summary>When it was marked as a gift; null for gifts made before this was tracked.</summary>
    public DateTime? GiftedAt { get; set; }
    public string GiftedBy { get; set; } = default!;
}

public class SalesReportTableLineDto
{
    public int TableId { get; set; }
    public string TableName { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    /// <summary>Each customer sitting at this table in the period, oldest first.</summary>
    public List<SalesReportReceiptDto> Sessions { get; set; } = new();
}
