namespace Domain.Enums;

public enum StockMovementType
{
    TransferOut = 1,
    TransferIn = 2,
    /// <summary>Stock increase from approved warehouse stock entry document.</summary>
    StockEntryIn = 3,
    /// <summary>Stock decrease due to menu item recipe consumption.</summary>
    OrderConsumptionOut = 4,
    /// <summary>Stock increase from reversing a recipe consumption (order line removed/cancelled).</summary>
    OrderConsumptionReversalIn = 5,
    /// <summary>Stock increase from a returned sale (Geri qaytarma) with restocking.</summary>
    SaleReturnIn = 6,
    /// <summary>Stock increase from an approved supplier purchase (stok alışı).</summary>
    PurchaseIn = 7,
    /// <summary>Stock decrease from cancelling an already approved supplier purchase.</summary>
    PurchaseReversalOut = 8,
    /// <summary>Stock decrease from goods written off as waste, spoiled or broken.</summary>
    WriteOffOut = 9,
    /// <summary>Stock decrease from goods handed back to the supplier.</summary>
    PurchaseReturnOut = 10,
}
