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
}
