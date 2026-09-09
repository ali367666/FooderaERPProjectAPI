using Domain.Enums;

namespace Application.Common.Helpers;

/// <summary>
/// Canonical LineTotal formula, shared by every place that recomputes it (payment, receipt,
/// order queries, line updates). Keeping it in one place avoids the handlers drifting apart.
/// </summary>
public static class OrderLinePricing
{
    /// <summary>
    /// Weight-sold items (Kg unit) store Quantity in GRAMS so a scale reading like 0.850 kg
    /// can be recorded exactly as an integer (850) without a database schema change. The
    /// item's Price/StationPrice is still "per kg", so the raw amount divides by 1000.
    /// Gram-unit items already price "per gram", so Quantity (grams) multiplies directly.
    /// </summary>
    public static decimal ComputeLineTotal(Domain.Entities.OrderLine line)
    {
        if (line.MenuItem?.IsTimeBased == true)
            return line.LineTotal;

        var isKg = line.MenuItem?.UnitId == (int)UnitOfMeasure.Kg;
        var rawAmount = isKg
            ? line.UnitPrice * line.Quantity / 1000m
            : line.UnitPrice * line.Quantity;

        return line.IsGift ? 0m : Math.Max(0, rawAmount - line.DiscountAmount);
    }

    public static bool IsWeightBased(int? unitId) =>
        unitId == (int)UnitOfMeasure.Kg || unitId == (int)UnitOfMeasure.Gram;
}
