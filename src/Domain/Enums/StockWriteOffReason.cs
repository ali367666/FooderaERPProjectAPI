namespace Domain.Enums;

/// <summary>Why stock was written off (taken out of the warehouse without a sale or a return to the supplier).</summary>
public enum StockWriteOffReason
{
    /// <summary>Thrown away: unusable leftovers, trimmings.</summary>
    Waste = 1,
    /// <summary>Went bad.</summary>
    Spoiled = 2,
    /// <summary>Broken or dropped.</summary>
    Broken = 3,
    /// <summary>Passed its expiry date.</summary>
    Expired = 4,
    Other = 5
}
