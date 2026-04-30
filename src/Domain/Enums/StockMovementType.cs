namespace Domain.Enums;

public enum StockMovementType
{
    TransferOut = 1,
    TransferIn = 2,
    /// <summary>Stock increase from approved warehouse stock entry document.</summary>
    StockEntryIn = 3,
    /// <summary>Stock decrease due to menu item recipe consumption.</summary>
    OrderConsumptionOut = 4,
}
