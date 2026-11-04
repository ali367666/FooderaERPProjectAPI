using Domain.Enums;

namespace Application.StoreMode;

// ---- Suppliers and the ledger --------------------------------------------------------------

public class SaveStoreSupplierRequest
{
    public string Name { get; set; } = default!;
    public string? LegalName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? Note { get; set; }
    public int PaymentTermDays { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>What the store already owed the supplier before using the system (new supplier only; recorded in the ledger).</summary>
    public decimal? OpeningBalance { get; set; }
}

public class StoreSupplierResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? LegalName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? Note { get; set; }
    public int PaymentTermDays { get; set; }
    public bool IsActive { get; set; }

    /// <summary>What the store owes the supplier (from the ledger); negative means the supplier owes the store (an advance).</summary>
    public decimal Balance { get; set; }
}

public class StoreLedgerEntryResponse
{
    public int Id { get; set; }
    public DateTime EntryDate { get; set; }
    public StoreLedgerEntryType Type { get; set; }
    public string TypeName { get; set; } = default!;

    /// <summary>Plus raises the balance (we owe more), minus lowers it.</summary>
    public decimal Amount { get; set; }
    public decimal RunningBalance { get; set; }
    public string? DocumentType { get; set; }
    public int? DocumentId { get; set; }
    public string? DocumentNo { get; set; }
    public string? Note { get; set; }
}

/// <summary>A supplier's account for a period: opening balance, every movement, closing balance.</summary>
public class StoreSupplierStatementResponse
{
    public StoreSupplierResponse Supplier { get; set; } = default!;
    public decimal OpeningBalance { get; set; }
    public decimal TotalIncrease { get; set; }
    public decimal TotalDecrease { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<StoreLedgerEntryResponse> Entries { get; set; } = new();
    public List<StoreSupplierPaymentResponse> Payments { get; set; } = new();
}

public class CreateStoreSupplierPaymentRequest
{
    public int SupplierId { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Cash, Card or BankTransfer.</summary>
    public StorePaymentMethod Method { get; set; } = StorePaymentMethod.Cash;
    public DateTime? PaymentDate { get; set; }
    public string? Reference { get; set; }
    public string? Note { get; set; }

    /// <summary>Which purchases it pays. Left out, the oldest unpaid purchases are paid first; any excess stays as an advance.</summary>
    public List<StorePaymentAllocationRequest>? Allocations { get; set; }
}

public class StorePaymentAllocationRequest
{
    public int PurchaseId { get; set; }
    public decimal Amount { get; set; }
}

public class StoreSupplierPaymentResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public StorePaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public string? Note { get; set; }
    public decimal Allocated { get; set; }
    public bool IsReversed { get; set; }
    public string? ReverseReason { get; set; }

    /// <summary>The part not tied to any purchase: an advance on the supplier's account.</summary>
    public decimal Advance { get; set; }
    public List<StorePaymentAllocationResponse> Allocations { get; set; } = new();
}

public class StorePaymentAllocationResponse
{
    public int PurchaseId { get; set; }
    public string PurchaseDocumentNo { get; set; } = default!;
    public decimal Amount { get; set; }
}

/// <summary>Per supplier: what is owed and how overdue it is, from the unpaid confirmed purchases.</summary>
public class StoreSupplierDebtResponse
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = default!;
    public decimal Balance { get; set; }
    public decimal Advance { get; set; }
    public decimal NotDue { get; set; }
    public decimal Overdue1To30 { get; set; }
    public decimal Overdue31To60 { get; set; }
    public decimal Overdue61To90 { get; set; }
    public decimal Overdue90Plus { get; set; }
    public decimal TotalOverdue => Overdue1To30 + Overdue31To60 + Overdue61To90 + Overdue90Plus;
    public DateTime? LastPaymentDate { get; set; }
    public List<StoreOpenPurchaseResponse> OpenPurchases { get; set; } = new();
}

public class StoreOpenPurchaseResponse
{
    public int PurchaseId { get; set; }
    public string DocumentNo { get; set; } = default!;
    public string? SupplierInvoiceNo { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Total { get; set; }
    public decimal Paid { get; set; }
    public decimal Returned { get; set; }
    public decimal Outstanding { get; set; }
    public int DaysOverdue { get; set; }
}

public class StoreSupplierDebtReportResponse
{
    public decimal TotalOwed { get; set; }
    public decimal TotalOverdue { get; set; }
    public decimal TotalAdvance { get; set; }
    public List<StoreSupplierDebtResponse> Suppliers { get; set; } = new();
}

// ---- Purchases -----------------------------------------------------------------------------

public class SaveStorePurchaseRequest
{
    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public string? SupplierInvoiceNo { get; set; }
    public DateTime? PurchaseDate { get; set; }

    /// <summary>When it must be paid; left out it is the purchase date plus the supplier's payment term.</summary>
    public DateTime? DueDate { get; set; }
    public string? Note { get; set; }
    public List<SaveStorePurchaseLineRequest> Lines { get; set; } = new();
}

public class SaveStorePurchaseLineRequest
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>Price per unit as invoiced, before VAT.</summary>
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }

    /// <summary>VAT percent added on top; left out, the product's own VAT percent is used.</summary>
    public decimal? VatPercent { get; set; }

    /// <summary>IMEI / serial numbers received (for tracked products), as many as the quantity.</summary>
    public List<string>? SerialNumbers { get; set; }
}

public class ConfirmStorePurchaseRequest
{
    /// <summary>Optional: money paid to the supplier at once for this purchase.</summary>
    public ConfirmPurchasePayment? Payment { get; set; }
}

public class ConfirmPurchasePayment
{
    public decimal Amount { get; set; }
    public StorePaymentMethod Method { get; set; } = StorePaymentMethod.Cash;
    public string? Reference { get; set; }
}

public class CancelStorePurchaseRequest
{
    public string Reason { get; set; } = default!;
}

public class ReturnStorePurchaseRequest
{
    public string Reason { get; set; } = default!;
    public List<ReturnStorePurchaseLineRequest> Lines { get; set; } = new();
}

public class ReturnStorePurchaseLineRequest
{
    public int PurchaseLineId { get; set; }
    public decimal Quantity { get; set; }
    public List<string>? SerialNumbers { get; set; }
}

public class StorePurchaseLineResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public bool TracksSerial { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal VatPercent { get; set; }
    public decimal LineTotal { get; set; }
    public decimal NetUnitCost { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal ReturnableQuantity { get; set; }
    public List<string> SerialNumbers { get; set; } = new();
}

public class StorePurchaseReturnResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public string Reason { get; set; } = default!;
    public decimal Total { get; set; }
    public List<StorePurchaseReturnLineResponse> Lines { get; set; } = new();
}

public class StorePurchaseReturnLineResponse
{
    public string ProductName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
}

public class StorePurchaseResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = default!;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public string? SupplierInvoiceNo { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? DueDate { get; set; }
    public StorePurchaseStatus Status { get; set; }
    public string? Note { get; set; }
    public decimal Total { get; set; }
    public decimal VatTotal { get; set; }
    public decimal Paid { get; set; }
    public decimal Returned { get; set; }

    /// <summary>Total minus returns minus payments (never below zero).</summary>
    public decimal Outstanding { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public string? ConfirmedByName { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancelReason { get; set; }
    public List<StorePurchaseLineResponse> Lines { get; set; } = new();
    public List<StorePurchaseReturnResponse> Returns { get; set; } = new();
}

public class StorePurchaseListItemResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public string SupplierName { get; set; } = default!;
    public string? SupplierInvoiceNo { get; set; }
    public DateTime PurchaseDate { get; set; }
    public StorePurchaseStatus Status { get; set; }
    public decimal Total { get; set; }
    public decimal Paid { get; set; }
    public decimal Outstanding { get; set; }
}

public class StorePurchasePageResponse
{
    public int Total { get; set; }
    public List<StorePurchaseListItemResponse> Items { get; set; } = new();
}

public class StorePriceHistoryResponse
{
    public StorePriceType PriceType { get; set; }
    public string PriceTypeName { get; set; } = default!;
    public decimal? OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public string? ChangedByName { get; set; }
    public string? Reason { get; set; }
}
