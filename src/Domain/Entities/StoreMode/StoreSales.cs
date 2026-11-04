using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.StoreMode;

/// <summary>A till: belongs to a branch and sells from one warehouse.</summary>
public class StoreCashRegister : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

/// <summary>A cashier's working period on one till: opened with a starting cash amount, closed with a count.</summary>
public class StoreCashShift : CompanyEntity<int>
{
    public int StoreCashRegisterId { get; set; }
    public StoreCashRegister StoreCashRegister { get; set; } = default!;

    public int RestaurantId { get; set; }
    public int CashierUserId { get; set; }

    public StoreShiftStatus Status { get; set; } = StoreShiftStatus.Open;

    public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;
    public decimal OpeningCash { get; set; }

    public DateTime? ClosedAtUtc { get; set; }
    public int? ClosedByUserId { get; set; }

    /// <summary>What the till should hold when the shift was closed.</summary>
    public decimal? ExpectedCash { get; set; }

    /// <summary>What was counted in the till at closing.</summary>
    public decimal? ActualCash { get; set; }
    public decimal? CashDifference { get; set; }
    public string? CloseNote { get; set; }
}

/// <summary>Cash put into or taken out of the till outside a sale; counted in the shift's reports.</summary>
public class StoreCashMovement : CompanyEntity<int>
{
    public int StoreCashShiftId { get; set; }
    public StoreCashShift StoreCashShift { get; set; } = default!;

    public StoreCashMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = default!;

    /// <summary>A manager who approved it, when approval was required.</summary>
    public int? ApprovedByUserId { get; set; }
}

/// <summary>
/// A completed sale. Created in one transaction together with its stock movements; never deleted, only
/// cancelled. <see cref="OperationId"/> makes a resent request create nothing twice.
/// </summary>
public class StoreSale : CompanyEntity<int>
{
    public string DocumentNo { get; set; } = default!;

    /// <summary>Sent by the client; a second sale with the same id is refused and the first returned.</summary>
    public Guid OperationId { get; set; }

    public int RestaurantId { get; set; }
    public int WarehouseId { get; set; }
    public int StoreCashRegisterId { get; set; }
    public StoreCashRegister StoreCashRegister { get; set; } = default!;
    public int StoreCashShiftId { get; set; }
    public StoreCashShift StoreCashShift { get; set; } = default!;

    public StoreSaleStatus Status { get; set; } = StoreSaleStatus.Completed;

    /// <summary>The customer card the sale was made on (bonus, credit); null for an anonymous sale.</summary>
    public int? StoreCustomerId { get; set; }
    public StoreCustomer? StoreCustomer { get; set; }

    /// <summary>Bonus earned on the sale, bonus paid with, and the part left on the customer's account (AZN).</summary>
    public decimal BonusEarned { get; set; }
    public decimal BonusSpent { get; set; }
    public decimal DebtAmount { get; set; }

    /// <summary>Sum of the lines before any discount (AZN).</summary>
    public decimal SubTotal { get; set; }
    public decimal DiscountTotal { get; set; }

    /// <summary>VAT contained in <see cref="Total"/>.</summary>
    public decimal VatTotal { get; set; }
    public decimal Total { get; set; }

    /// <summary>All payments taken, including the cash handed over.</summary>
    public decimal PaidTotal { get; set; }

    /// <summary>Cash given back to the customer.</summary>
    public decimal ChangeAmount { get; set; }

    public string? Note { get; set; }

    public DateTime? CancelledAtUtc { get; set; }
    public int? CancelledByUserId { get; set; }
    public string? CancelReason { get; set; }

    /// <summary>Where the sale stands with the fiscal device; the sale itself stands whatever the device answers.</summary>
    public StoreFiscalStatus FiscalStatus { get; set; } = StoreFiscalStatus.NotRequired;

    /// <summary>Sold without printing a receipt (allowed by the company rule and the cashier's permission); the sale is recorded all the same.</summary>
    public bool IsReceiptless { get; set; }
    public string? FiscalReceiptNo { get; set; }
    public string? FiscalError { get; set; }

    public ICollection<StoreSaleLine> Lines { get; set; } = new List<StoreSaleLine>();
    public ICollection<StoreSalePayment> Payments { get; set; } = new List<StoreSalePayment>();
}

/// <summary>
/// One product on a sale. Name, code, price, VAT and cost are copied here, so later edits of the product
/// never change an old sale.
/// </summary>
public class StoreSaleLine : BaseEntity<int>
{
    public int StoreSaleId { get; set; }
    public StoreSale StoreSale { get; set; } = default!;

    public int StoreProductId { get; set; }
    public StoreProduct StoreProduct { get; set; } = default!;

    public string ProductName { get; set; } = default!;
    public string? Barcode { get; set; }
    public string? Sku { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>The product's list price when sold; differs from <see cref="UnitPrice"/> when the price was changed at the till.</summary>
    public decimal ListPrice { get; set; }
    public string? PriceChangeReason { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatPercent { get; set; }
    public decimal VatAmount { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>The product's average cost per unit when it was sold.</summary>
    public decimal UnitCost { get; set; }
}

public class StoreSalePayment : BaseEntity<int>
{
    public int StoreSaleId { get; set; }
    public StoreSale StoreSale { get; set; } = default!;

    public StorePaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
}
