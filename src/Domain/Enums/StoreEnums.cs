namespace Domain.Enums;

/// <summary>Why a store product's stock changed. Every change is one ledger row.</summary>
public enum StoreStockMovementType
{
    Purchase = 1,
    Sale = 2,
    SaleReturn = 3,
    PurchaseReturn = 4,
    TransferOut = 5,
    TransferIn = 6,
    AdjustmentIncrease = 7,
    AdjustmentDecrease = 8,
    Waste = 9,
    InternalUse = 10,
    /// <summary>Stock put back because a completed sale was cancelled.</summary>
    SaleCancel = 11
}

public enum StoreSaleStatus
{
    Completed = 1,
    Cancelled = 2
}

public enum StorePaymentMethod
{
    Cash = 1,
    Card = 2,
    Bonus = 3,
    GiftCard = 4,
    Debt = 5,
    /// <summary>Value of goods handed back in an exchange, used up as payment for the new goods.</summary>
    ReturnCredit = 6,
    /// <summary>Paying a supplier by bank transfer (not used for sales).</summary>
    BankTransfer = 7
}

public enum StoreSerialStatus
{
    InStock = 1,
    Sold = 2,
    WrittenOff = 3,
    /// <summary>Returned by a customer and waiting to be checked; not for sale yet.</summary>
    Inspection = 4,
    /// <summary>Shipped by a transfer and not yet received at the other warehouse.</summary>
    InTransit = 5
}

public enum StoreSerialEventType
{
    Received = 1,
    Sold = 2,
    Returned = 3,
    WrittenOff = 4,
    TransferOut = 5,
    TransferIn = 6,
    Service = 7
}

/// <summary>State of goods handed back by a customer, which decides whether they go back on the shelf.</summary>
public enum StoreReturnCondition
{
    /// <summary>Fit to sell again: goes back into stock.</summary>
    Sellable = 1,
    /// <summary>Unusable: not restocked, recorded as waste.</summary>
    Waste = 2,
    /// <summary>Needs checking first: not restocked yet.</summary>
    Inspect = 3
}

public enum StoreShiftStatus
{
    Open = 1,
    Closed = 2
}

public enum StoreCashMovementType
{
    /// <summary>Cash put into the till outside a sale.</summary>
    In = 1,
    /// <summary>Cash taken out of the till outside a sale.</summary>
    Out = 2
}

public enum StorePurchaseStatus
{
    /// <summary>Not yet received: changes neither stock nor the supplier's balance.</summary>
    Draft = 0,
    Confirmed = 1,
    Cancelled = 2
}

public enum StoreLedgerPartyType
{
    Supplier = 1,
    Customer = 2
}

public enum StoreLedgerEntryType
{
    OpeningBalance = 1,
    Purchase = 2,
    PurchaseReturn = 3,
    SupplierPayment = 4,
    Adjustment = 5,
    PurchaseCancel = 6,
    CreditSale = 7,
    CustomerPayment = 8,
    SaleReturnCredit = 9,
    /// <summary>A payment cancelled by a counter entry.</summary>
    PaymentReversal = 10
}

public enum StorePriceType
{
    Sale = 1,
    Purchase = 2,
    Wholesale = 3
}

public enum StoreInventoryStatus
{
    /// <summary>Counting is under way; stock is untouched.</summary>
    Draft = 0,
    Confirmed = 1,
    Cancelled = 2
}

public enum StoreTransferStatus
{
    Draft = 0,
    Pending = 1,
    Shipped = 2,
    PartiallyReceived = 3,
    Received = 4,
    Cancelled = 5
}

public enum StoreBonusEntryType
{
    /// <summary>Earned from a sale.</summary>
    Earned = 1,
    /// <summary>Paid with on a sale.</summary>
    Spent = 2,
    /// <summary>Taken back because the goods it was earned on were returned or the sale cancelled.</summary>
    Reversal = 3,
    /// <summary>Given back because the goods it paid for were returned or the sale cancelled.</summary>
    Refund = 4,
    /// <summary>Corrected by hand with a reason.</summary>
    Adjustment = 5,
    /// <summary>Written off because it stayed unused longer than the company's bonus period.</summary>
    Expired = 6
}

/// <summary>Where a sale stands with the fiscal device.</summary>
public enum StoreFiscalStatus
{
    /// <summary>The company does not use a fiscal device.</summary>
    NotRequired = 0,
    /// <summary>Waiting to be sent to the device.</summary>
    Pending = 1,
    /// <summary>The device confirmed it and gave a fiscal receipt number.</summary>
    Confirmed = 2,
    /// <summary>The device refused or could not be reached; the sale stands and can be sent again.</summary>
    Failed = 3
}

/// <summary>Stage of an operation a till device sent for syncing.</summary>
public enum StoreSyncStatus
{
    Pending = 0,
    Syncing = 1,
    Synced = 2,
    /// <summary>Refused for a reason that needs a person (not enough stock, serial gone...); kept with the reason.</summary>
    Conflict = 3,
    /// <summary>Failed for another reason; kept and can be tried again.</summary>
    Failed = 4
}
