using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Helpers;

/// <summary>
/// The arithmetic behind part payments, shared by the pay command, the "payments" query and the
/// receipt so they can never disagree by a cent. The order-level discount is spread over every
/// line in proportion, and whatever rounding leaves over lands on the final payment.
/// </summary>
public static class OrderPaymentMath
{
    public static List<OrderLine> ActiveLines(Domain.Entities.Order order)
    {
        var lines = order.Lines
            .DistinctBy(x => x.Id)
            .Where(x => x.Status != OrderLineStatus.Cancelled)
            .ToList();
        foreach (var line in lines)
            line.LineTotal = OrderLinePricing.ComputeLineTotal(line);
        return lines;
    }

    public static decimal Subtotal(IEnumerable<OrderLine> lines) => lines.Sum(x => x.LineTotal);

    /// <summary>Share of each line's price left after the order-level discount.</summary>
    public static decimal DiscountFactor(decimal subtotal, decimal discount) =>
        subtotal <= 0 ? 1m : Math.Max(0, subtotal - discount) / subtotal;

    /// <summary>Weight-sold and time-billed items can only be paid for whole.</summary>
    public static bool IsAtomic(OrderLine line) =>
        line.MenuItem?.IsTimeBased == true || line.MenuItem?.UnitId == (int)UnitOfMeasure.Kg;

    public static decimal Portion(OrderLine line, int quantity, decimal factor) =>
        line.Quantity <= 0 ? 0m : Math.Round(line.LineTotal * quantity / line.Quantity * factor, 2, MidpointRounding.AwayFromZero);

    /// <summary>Bill without service charge: lines − discount + table rental.</summary>
    public static decimal BillTotal(Domain.Entities.Order order, decimal subtotal) =>
        Math.Max(0, subtotal - order.DiscountAmount + (order.TableRentalAmount ?? 0));

    public static Dictionary<int, int> PaidQuantities(IEnumerable<OrderPayment> payments) =>
        payments
            .SelectMany(p => p.Lines)
            .GroupBy(l => l.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
}
