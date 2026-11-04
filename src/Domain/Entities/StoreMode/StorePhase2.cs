using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.StoreMode;

/// <summary>One of the 50 quick-sale buttons of a till: a product shown as a big button, with an optional own label and colour.</summary>
public class StoreQuickSlot : CompanyEntity<int>
{
    public int StoreCashRegisterId { get; set; }
    public StoreCashRegister StoreCashRegister { get; set; } = default!;

    /// <summary>1 to 50.</summary>
    public int SlotNumber { get; set; }

    public int? StoreProductId { get; set; }
    public StoreProduct? StoreProduct { get; set; }

    public string? Label { get; set; }
    public string? Color { get; set; }
}

/// <summary>A basket set aside at the till (hold) to be picked up again later. It has no effect on stock or money.</summary>
public class StoreHeldSale : CompanyEntity<int>
{
    public int StoreCashRegisterId { get; set; }
    public StoreCashRegister StoreCashRegister { get; set; } = default!;

    public int CashierUserId { get; set; }
    public string? Title { get; set; }

    /// <summary>The basket as the till saved it (product ids and quantities).</summary>
    public string CartJson { get; set; } = default!;
    public int ItemCount { get; set; }
    public decimal Total { get; set; }
}

/// <summary>One physical unit of a serial / IMEI tracked product. The same number can exist only once per company.</summary>
public class StoreSerialUnit : CompanyEntity<int>
{
    public int StoreProductId { get; set; }
    public StoreProduct StoreProduct { get; set; } = default!;

    public string SerialNumber { get; set; } = default!;

    /// <summary>Where the unit is while in stock.</summary>
    public int? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public StoreSerialStatus Status { get; set; } = StoreSerialStatus.InStock;

    /// <summary>The sale line the unit left on.</summary>
    public int? StoreSaleLineId { get; set; }
    public StoreSaleLine? StoreSaleLine { get; set; }
    public DateTime? SoldAtUtc { get; set; }

    public ICollection<StoreSerialEvent> Events { get; set; } = new List<StoreSerialEvent>();
}

/// <summary>The history of a serial unit: received, sold, returned, written off... with the document behind each step.</summary>
public class StoreSerialEvent : BaseEntity<int>
{
    public int StoreSerialUnitId { get; set; }
    public StoreSerialUnit StoreSerialUnit { get; set; } = default!;

    public StoreSerialEventType Type { get; set; }
    public int? WarehouseId { get; set; }
    public string? DocumentType { get; set; }
    public int? DocumentId { get; set; }
    public string? DocumentNo { get; set; }
    public string? Note { get; set; }
}

/// <summary>A manager's approval of a sensitive step (big discount, price change, cancel, return): who approved, why, when.</summary>
public class StoreApprovalLog : CompanyEntity<int>
{
    public string Action { get; set; } = default!;
    public string? DocumentType { get; set; }
    public int? DocumentId { get; set; }
    public string? DocumentNo { get; set; }

    /// <summary>The manager who approved. The cashier who asked is <c>CreatedByUserId</c>.</summary>
    public int ApprovedByUserId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>Goods handed back against an earlier sale. It points at that sale and cannot return more than was sold.</summary>
public class StoreSaleReturn : CompanyEntity<int>
{
    public string DocumentNo { get; set; } = default!;
    public Guid OperationId { get; set; }

    public int StoreSaleId { get; set; }
    public StoreSale StoreSale { get; set; } = default!;

    public int RestaurantId { get; set; }
    public int WarehouseId { get; set; }
    public int StoreCashRegisterId { get; set; }
    public StoreCashRegister StoreCashRegister { get; set; } = default!;

    /// <summary>The shift of the till the goods were taken back at (its cash is what the refund comes out of).</summary>
    public int StoreCashShiftId { get; set; }
    public StoreCashShift StoreCashShift { get; set; } = default!;

    public string Reason { get; set; } = default!;

    /// <summary>Value of the returned goods (AZN).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>What was paid back in cash or to the card; less than the total when part of it paid for new goods in an exchange.</summary>
    public decimal RefundAmount { get; set; }
    public StorePaymentMethod RefundMethod { get; set; } = StorePaymentMethod.Cash;

    /// <summary>The part of the value that cancels the customer's debt instead of being paid out.</summary>
    public decimal DebtCreditAmount { get; set; }

    /// <summary>Bonus handed back (what the returned goods were paid with) and bonus taken back (what they earned).</summary>
    public decimal BonusRefundAmount { get; set; }
    public decimal BonusClawbackAmount { get; set; }

    /// <summary>The new sale when this return was part of an exchange.</summary>
    public int? ExchangeSaleId { get; set; }

    public ICollection<StoreSaleReturnLine> Lines { get; set; } = new List<StoreSaleReturnLine>();
}

public class StoreSaleReturnLine : BaseEntity<int>
{
    public int StoreSaleReturnId { get; set; }
    public StoreSaleReturn StoreSaleReturn { get; set; } = default!;

    public int StoreSaleLineId { get; set; }
    public StoreSaleLine StoreSaleLine { get; set; } = default!;

    public int StoreProductId { get; set; }
    public string ProductName { get; set; } = default!;

    public decimal Quantity { get; set; }

    /// <summary>What one unit is worth back: the line's total after discount divided by its quantity.</summary>
    public decimal UnitAmount { get; set; }
    public decimal Amount { get; set; }
    public decimal UnitCost { get; set; }

    public StoreReturnCondition Condition { get; set; } = StoreReturnCondition.Sellable;
}
