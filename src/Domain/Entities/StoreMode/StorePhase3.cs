using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.StoreMode;

/// <summary>A supplier card. What the store owes it is worked out from the ledger, never typed in as one number.</summary>
public class StoreSupplier : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public string? LegalName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? Note { get; set; }

    /// <summary>Days after a purchase when it is due; used by the overdue and aging reports.</summary>
    public int PaymentTermDays { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Goods received from a supplier. A draft changes nothing; confirming it puts the goods in stock, updates
/// the average cost and adds the amount to what the store owes the supplier.
/// </summary>
public class StorePurchase : CompanyEntity<int>
{
    public string DocumentNo { get; set; } = default!;

    public int StoreSupplierId { get; set; }
    public StoreSupplier StoreSupplier { get; set; } = default!;

    /// <summary>The supplier's own invoice number, for matching the paper document.</summary>
    public string? SupplierInvoiceNo { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public StorePurchaseStatus Status { get; set; } = StorePurchaseStatus.Draft;
    public string? Note { get; set; }

    /// <summary>Sum of the lines after their discounts, VAT included (AZN).</summary>
    public decimal Total { get; set; }
    public decimal VatTotal { get; set; }

    public DateTime? ConfirmedAtUtc { get; set; }
    public int? ConfirmedByUserId { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public int? CancelledByUserId { get; set; }
    public string? CancelReason { get; set; }

    public ICollection<StorePurchaseLine> Lines { get; set; } = new List<StorePurchaseLine>();
}

public class StorePurchaseLine : BaseEntity<int>
{
    public int StorePurchaseId { get; set; }
    public StorePurchase StorePurchase { get; set; } = default!;

    public int StoreProductId { get; set; }
    public StoreProduct StoreProduct { get; set; } = default!;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Discount on the line, in percent.</summary>
    public decimal DiscountPercent { get; set; }
    public decimal VatPercent { get; set; }

    /// <summary>Quantity times price less the discount, VAT included.</summary>
    public decimal LineTotal { get; set; }

    /// <summary>What one unit really cost after the discount: the figure the average cost is worked from.</summary>
    public decimal NetUnitCost { get; set; }

    /// <summary>Serial / IMEI numbers received on this line (for tracked products), comma separated.</summary>
    public string? SerialNumbers { get; set; }
}

/// <summary>Goods handed back to a supplier from a confirmed purchase: stock drops, the debt shrinks.</summary>
public class StorePurchaseReturn : CompanyEntity<int>
{
    public string DocumentNo { get; set; } = default!;

    public int StorePurchaseId { get; set; }
    public StorePurchase StorePurchase { get; set; } = default!;

    public int StoreSupplierId { get; set; }
    public int WarehouseId { get; set; }

    public string Reason { get; set; } = default!;
    public decimal Total { get; set; }

    public ICollection<StorePurchaseReturnLine> Lines { get; set; } = new List<StorePurchaseReturnLine>();
}

public class StorePurchaseReturnLine : BaseEntity<int>
{
    public int StorePurchaseReturnId { get; set; }
    public StorePurchaseReturn StorePurchaseReturn { get; set; } = default!;

    public int StorePurchaseLineId { get; set; }
    public StorePurchaseLine StorePurchaseLine { get; set; } = default!;

    public int StoreProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitAmount { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>
/// Money paid to a supplier. It is spread over one or more purchases (<see cref="Allocations"/>); what is left
/// over stays as an advance on the supplier's account.
/// </summary>
public class StoreSupplierPayment : CompanyEntity<int>
{
    public string DocumentNo { get; set; } = default!;

    public int StoreSupplierId { get; set; }
    public StoreSupplier StoreSupplier { get; set; } = default!;

    public decimal Amount { get; set; }
    public StorePaymentMethod Method { get; set; } = StorePaymentMethod.Cash;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    /// <summary>The bank reference or receipt number.</summary>
    public string? Reference { get; set; }
    public string? Note { get; set; }

    public ICollection<StorePaymentAllocation> Allocations { get; set; } = new List<StorePaymentAllocation>();

    /// <summary>Set when the payment was cancelled (by a counter ledger entry); the payment itself is never deleted.</summary>
    public DateTime? ReversedAtUtc { get; set; }
    public int? ReversedByUserId { get; set; }
    public string? ReverseReason { get; set; }
}

public class StorePaymentAllocation : BaseEntity<int>
{
    public int StoreSupplierPaymentId { get; set; }
    public StoreSupplierPayment StoreSupplierPayment { get; set; } = default!;

    public int StorePurchaseId { get; set; }
    public StorePurchase StorePurchase { get; set; } = default!;

    public decimal Amount { get; set; }
}

/// <summary>
/// One financial movement on a supplier's (or customer's) account. <see cref="Amount"/> is signed: plus raises
/// what the account owes, minus lowers it. The balance is the sum of the entries and can be re-derived and audited.
/// </summary>
public class StoreLedgerEntry : CompanyEntity<int>
{
    public StoreLedgerPartyType PartyType { get; set; }
    public int PartyId { get; set; }

    public StoreLedgerEntryType Type { get; set; }
    public decimal Amount { get; set; }

    public DateTime EntryDate { get; set; } = DateTime.UtcNow;

    public string? DocumentType { get; set; }
    public int? DocumentId { get; set; }
    public string? DocumentNo { get; set; }
    public string? Note { get; set; }
}

/// <summary>Every change of a product's price: what it was, what it became, when, by whom and why.</summary>
public class StorePriceHistory : CompanyEntity<int>
{
    public int StoreProductId { get; set; }
    public StoreProduct StoreProduct { get; set; } = default!;

    public StorePriceType PriceType { get; set; }
    public decimal? OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}
