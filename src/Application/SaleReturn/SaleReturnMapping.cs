using Application.Common.Helpers;
using Domain.Enums;

namespace Application.SaleReturn;

internal static class SaleReturnMapping
{
    /// <summary>
    /// Share of the line subtotal the customer actually paid — the order-level discount is spread
    /// proportionally over the lines. Service charge and table rental are never refunded.
    /// </summary>
    public static decimal PaidFactor(Domain.Entities.Order order)
    {
        var subtotal = ActiveLines(order).Sum(x => x.LineTotal);
        if (subtotal <= 0)
            return 0;
        return Math.Clamp((subtotal - order.DiscountAmount) / subtotal, 0, 1);
    }

    public static IEnumerable<Domain.Entities.OrderLine> ActiveLines(Domain.Entities.Order order) =>
        order.Lines.DistinctBy(x => x.Id).Where(x => x.Status != OrderLineStatus.Cancelled);

    public static decimal RefundUnitAmount(Domain.Entities.OrderLine line, decimal paidFactor) =>
        line.Quantity > 0 ? line.LineTotal / line.Quantity * paidFactor : 0;

    public static ReturnableOrderResponse MapOrder(
        Domain.Entities.Order order, List<Domain.Entities.SaleReturn> returns)
    {
        var factor = PaidFactor(order);
        return new ReturnableOrderResponse
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            ReceiptNumber = order.ReceiptNumber,
            RestaurantId = order.RestaurantId,
            RestaurantName = order.Restaurant?.Name,
            TableName = order.Table?.Name,
            WaiterName = order.Waiter != null ? $"{order.Waiter.FirstName} {order.Waiter.LastName}" : null,
            CounterpartyName = order.Counterparty?.Name,
            PaidAt = order.PaidAt,
            PaymentMethod = order.PaymentMethod?.ToString(),
            TotalAmount = order.TotalAmount,
            ReturnedAmount = returns.Sum(x => x.TotalAmount),
            Lines = ActiveLines(order)
                .OrderBy(x => x.Id)
                .Select(x => new ReturnableOrderLineResponse
                {
                    OrderLineId = x.Id,
                    MenuItemId = x.MenuItemId,
                    MenuItemName = x.MenuItem?.Name ?? "?",
                    IsWeightBased = x.MenuItem != null && OrderLinePricing.IsWeightBased(x.MenuItem.UnitId),
                    Quantity = x.Quantity,
                    ReturnedQuantity = x.ReturnedQuantity,
                    ReturnableQuantity = Math.Max(0, x.Quantity - x.ReturnedQuantity),
                    LineTotal = x.LineTotal,
                    RefundUnitAmount = Math.Round(RefundUnitAmount(x, factor), 4)
                })
                .ToList(),
            Returns = returns.Select(MapReturn).ToList()
        };
    }

    public static SaleReturnResponse MapReturn(Domain.Entities.SaleReturn r) => new()
    {
        Id = r.Id,
        ReturnNumber = r.ReturnNumber,
        OrderId = r.OrderId,
        OrderNumber = r.Order?.OrderNumber,
        PaymentMethod = r.PaymentMethod.ToString(),
        TotalAmount = r.TotalAmount,
        RestockItems = r.RestockItems,
        Reason = r.Reason,
        CreatedByUserName = r.CreatedByUser?.FullName,
        CreatedAtUtc = r.CreatedAtUtc,
        Lines = r.Lines.Select(l => new SaleReturnLineResponse
        {
            OrderLineId = l.OrderLineId,
            MenuItemName = l.MenuItem?.Name ?? "?",
            Quantity = l.Quantity,
            Amount = l.Amount
        }).ToList()
    };
}
