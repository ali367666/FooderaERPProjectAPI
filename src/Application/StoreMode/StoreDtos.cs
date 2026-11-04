using Domain.Enums;

namespace Application.StoreMode;

// ---- Catalog -------------------------------------------------------------------------------

public class StoreCategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int? ParentCategoryId { get; set; }
    public bool IsActive { get; set; }
    public bool EarnsBonus { get; set; } = true;
}

public class SaveStoreCategoryRequest
{
    public string Name { get; set; } = default!;
    public int? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EarnsBonus { get; set; } = true;
}

public class StoreProductResponse
{
    /// <summary>Set when a weighed-goods barcode was scanned: the weight/amount encoded in it, in the product's unit.</summary>
    public decimal? ScannedQuantity { get; set; }

    /// <summary>What the till charges now (campaign price while it runs, else the sale price).</summary>
    public decimal EffectivePrice { get; set; }


    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Sku { get; set; }
    public string? PrimaryBarcode { get; set; }
    public List<string> Barcodes { get; set; } = new();
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Brand { get; set; }
    public UnitOfMeasure Unit { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal? WholesalePrice { get; set; }
    public decimal? CampaignPrice { get; set; }
    public DateTime? CampaignFromUtc { get; set; }
    public DateTime? CampaignToUtc { get; set; }
    public decimal? VatPercent { get; set; }
    public decimal AverageCost { get; set; }
    public decimal MinimumStock { get; set; }
    public bool TracksSerial { get; set; }
    public bool EarnsBonus { get; set; } = true;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Quantity in the warehouse asked for (the till's warehouse on the POS); null when none was asked.</summary>
    public decimal? Stock { get; set; }

    /// <summary>Supplier of the latest confirmed purchase of this product; filled on the product list only.</summary>
    public string? SupplierName { get; set; }
}

public class StoreProductPageResponse
{
    public int Total { get; set; }
    public List<StoreProductResponse> Items { get; set; } = new();
}

public class SaveStoreProductRequest
{
    public string Name { get; set; } = default!;
    public string? Sku { get; set; }

    /// <summary>All scan codes of the product; the first is the primary one.</summary>
    public List<string> Barcodes { get; set; } = new();
    public int? CategoryId { get; set; }
    public string? Brand { get; set; }
    public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Piece;
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal? WholesalePrice { get; set; }
    public decimal? CampaignPrice { get; set; }
    public DateTime? CampaignFromUtc { get; set; }
    public DateTime? CampaignToUtc { get; set; }
    public decimal? VatPercent { get; set; }
    public decimal MinimumStock { get; set; }
    public bool TracksSerial { get; set; }
    public bool EarnsBonus { get; set; } = true;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

// ---- Stock ---------------------------------------------------------------------------------

public class StoreStockBalanceResponse
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string? Sku { get; set; }
    public string? PrimaryBarcode { get; set; }
    public UnitOfMeasure Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinimumStock { get; set; }
    public bool IsLow { get; set; }
    public decimal AverageCost { get; set; }

    /// <summary>Quantity times average cost (AZN).</summary>
    public decimal Value { get; set; }
}

public class StoreStockMovementResponse
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedByUserName { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public StoreStockMovementType Type { get; set; }
    public string TypeName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal UnitCost { get; set; }
    public string? DocumentNo { get; set; }
    public string? Reason { get; set; }
}

public class AdjustStoreStockRequest
{
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }

    /// <summary>The movement: AdjustmentIncrease, AdjustmentDecrease, Waste or InternalUse.</summary>
    public StoreStockMovementType Type { get; set; }

    /// <summary>Always positive; the type decides whether stock goes up or down.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Cost per unit (AZN) of what is added; only used by AdjustmentIncrease to refresh the average cost.</summary>
    public decimal? UnitCost { get; set; }

    public string Reason { get; set; } = default!;

    /// <summary>Serial / IMEI numbers: the units added (increase) or taken out (decrease, waste, internal use) of a tracked product.</summary>
    public List<string>? SerialNumbers { get; set; }
}

public class StoreSettingsResponse
{
    public bool AllowNegativeStock { get; set; }

    /// <summary>A sale discount above this percent needs a manager's approval.</summary>
    public decimal MaxDiscountPercent { get; set; } = 10m;

    /// <summary>Bonus earned as a percent of what the customer really paid; 0 switches bonus off.</summary>
    public decimal BonusEarnPercent { get; set; }

    /// <summary>The most of a sale that may be paid with bonus, in percent.</summary>
    public decimal BonusMaxPaymentPercent { get; set; } = 50m;

    /// <summary>Bonus unused for this many days expires; 0 means never.</summary>
    public int BonusExpiryDays { get; set; }

    /// <summary>Sales are registered with a fiscal device.</summary>
    public bool FiscalEnabled { get; set; }

    public int ReceiptWidthMm { get; set; } = 80;
    public string? ReceiptHeader { get; set; }
    public string? ReceiptFooter { get; set; }
    public bool AllowReceiptlessSale { get; set; }
    public int QuickSlotCount { get; set; } = 50;
}

// ---- Registers and shifts ------------------------------------------------------------------

public class StoreCashRegisterResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = default!;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }

    /// <summary>The open shift on this till, if any.</summary>
    public int? OpenShiftId { get; set; }
    public string? OpenShiftCashierName { get; set; }
}

public class SaveStoreCashRegisterRequest
{
    public int RestaurantId { get; set; }
    public int WarehouseId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

public class OpenStoreShiftRequest
{
    public int StoreCashRegisterId { get; set; }
    public decimal OpeningCash { get; set; }
}

public class CloseStoreShiftRequest
{
    /// <summary>The cash counted in the till.</summary>
    public decimal ActualCash { get; set; }
    public string? Note { get; set; }
}

public class StoreCashMovementRequest
{
    public StoreCashMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = default!;

    /// <summary>A manager's credentials, when the cashier lacks the permission for this kind of cash movement.</summary>
    public StoreApprovalRequest? Approval { get; set; }
}

public class StoreShiftResponse
{
    public int Id { get; set; }
    public int StoreCashRegisterId { get; set; }
    public string RegisterName { get; set; } = default!;
    public int RestaurantId { get; set; }
    public int CashierUserId { get; set; }
    public string? CashierName { get; set; }
    public StoreShiftStatus Status { get; set; }
    public DateTime OpenedAtUtc { get; set; }
    public decimal OpeningCash { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }
    public decimal? CashDifference { get; set; }
}

public class StorePaymentTotalResponse
{
    public StorePaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
}

public class StoreCashMovementResponse
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public StoreCashMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = default!;
    public string? CreatedByUserName { get; set; }
}

/// <summary>X-Report while the shift is open (does not close it), Z-Report once it is closed.</summary>
public class StoreShiftReportResponse
{
    public StoreShiftResponse Shift { get; set; } = default!;

    /// <summary>True for a closed shift (Z-Report); false while it is open (X-Report).</summary>
    public bool IsFinal { get; set; }

    public int SalesCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal Discounts { get; set; }
    public decimal NetSales { get; set; }
    public decimal Vat { get; set; }

    public int CancelledCount { get; set; }
    public decimal CancelledTotal { get; set; }

    /// <summary>Money taken per method; cash is what stayed in the till (handed over minus change).</summary>
    public List<StorePaymentTotalResponse> Payments { get; set; } = new();

    public decimal CashIn { get; set; }
    public decimal CashOut { get; set; }
    public List<StoreCashMovementResponse> CashMovements { get; set; } = new();

    /// <summary>Debt payments taken at the till during the shift; the cash part lands in the till.</summary>
    public decimal CustomerPaymentsCash { get; set; }
    public decimal CustomerPaymentsCard { get; set; }

    public int ReturnsCount { get; set; }
    public decimal ReturnsTotal { get; set; }

    /// <summary>Refunds paid out of the till in cash.</summary>
    public decimal CashRefunds { get; set; }
    public decimal CardRefunds { get; set; }

    /// <summary>Opening cash + cash sales + cash in − cash out − cash refunds.</summary>
    public decimal ExpectedCash { get; set; }
}

// ---- Sales ---------------------------------------------------------------------------------

public class CreateStoreSaleRequest
{
    /// <summary>Generated by the client for this sale; resending the same id never creates a second sale.</summary>
    public Guid OperationId { get; set; }

    public int StoreCashRegisterId { get; set; }

    /// <summary>The customer card: needed to pay with bonus, sell on credit or earn bonus.</summary>
    public int? CustomerId { get; set; }
    public List<CreateStoreSaleLineRequest> Lines { get; set; } = new();
    public List<CreateStoreSalePaymentRequest> Payments { get; set; } = new();
    public string? Note { get; set; }

    /// <summary>Complete the sale without printing a receipt (needs the company rule and the cashier's permission).</summary>
    public bool Receiptless { get; set; }

    /// <summary>A manager's credentials, when the cashier lacks the permission for a discount or price change in this sale.</summary>
    public StoreApprovalRequest? Approval { get; set; }
}

public class CreateStoreSaleLineRequest
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>Discount on this line in AZN (needs the discount permission or a manager's approval).</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>A price other than the list price (needs the price-change permission or approval, and a reason).</summary>
    public decimal? OverridePrice { get; set; }
    public string? PriceChangeReason { get; set; }

    /// <summary>For a serial / IMEI product: the units sold, as many as the quantity.</summary>
    public List<string>? SerialNumbers { get; set; }
}

public class CreateStoreSalePaymentRequest
{
    public StorePaymentMethod Method { get; set; }

    /// <summary>For cash this is the amount handed over, change included.</summary>
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
}

public class CancelStoreSaleRequest
{
    public string Reason { get; set; } = default!;
    public StoreApprovalRequest? Approval { get; set; }
}

public class StoreSaleLineResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string? Barcode { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ListPrice { get; set; }
    public string? PriceChangeReason { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatPercent { get; set; }
    public decimal VatAmount { get; set; }
    public decimal LineTotal { get; set; }
    public int LineId { get; set; }
    public List<string> SerialNumbers { get; set; } = new();
}

public class StoreSalePaymentResponse
{
    public StorePaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
}

public class StoreSaleResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public Guid OperationId { get; set; }
    public StoreSaleStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int RestaurantId { get; set; }
    public string? BranchName { get; set; }
    public int StoreCashRegisterId { get; set; }
    public string? RegisterName { get; set; }
    public int StoreCashShiftId { get; set; }
    public int? CashierUserId { get; set; }
    public string? CashierName { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal BonusEarned { get; set; }
    public decimal BonusSpent { get; set; }
    public decimal DebtAmount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal Total { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? Note { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancelReason { get; set; }
    public bool IsReceiptless { get; set; }
    public StoreFiscalStatus FiscalStatus { get; set; }
    public string? FiscalReceiptNo { get; set; }
    public string? FiscalError { get; set; }
    public List<StoreSaleLineResponse> Lines { get; set; } = new();
    public List<StoreSalePaymentResponse> Payments { get; set; } = new();
}

public class StoreSaleListItemResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public StoreSaleStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CashierName { get; set; }
    public string? RegisterName { get; set; }
    public decimal Total { get; set; }
    public int LineCount { get; set; }
}

public class StoreSalePageResponse
{
    public int Total { get; set; }
    public List<StoreSaleListItemResponse> Items { get; set; } = new();
}
