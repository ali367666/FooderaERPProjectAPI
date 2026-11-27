using Application.Common.Helpers;
using Domain.Entities;
using Domain.Enums;

namespace Application.Orders.Services;

/// <summary>
/// Corrections of a paid receipt, worked out on the entities: another payment method, or another quantity of one line (the order's
/// totals, its payment and the payment's lines follow). The caller checks who may do it and saves.
/// </summary>
public static class ReceiptEditor
{
    /// <summary>Another method (cash or card) for a payment, or for the whole receipt when it carries no payment rows.</summary>
    public static void ApplyPaymentMethod(Domain.Entities.Order order, IReadOnlyList<OrderPayment> payments, OrderPayment? target, PaymentMethod method, Domain.Entities.Counterparty? bank)
    {
        if (target is not null)
        {
            var due = target.Amount + (target.ServiceChargeAmount ?? 0);
            target.Method = method;
            target.BankCounterpartyId = method == PaymentMethod.Card ? bank?.Id : null;
            // Card pays exactly what is due; cash keeps what was handed over when that covers it.
            target.ReceivedAmount = method == PaymentMethod.Card ? due : Math.Max(target.ReceivedAmount, due);
            target.ChangeAmount = method == PaymentMethod.Card ? 0 : target.ReceivedAmount - due;
        }

        // The receipt's own method follows when there is one payment (or none).
        if (payments.Count <= 1)
        {
            order.PaymentMethod = method;
            order.BankCounterpartyId = method == PaymentMethod.Card ? bank?.Id : null;
            if (target is not null)
            {
                order.PaidAmount = target.ReceivedAmount;
                order.ChangeAmount = target.ChangeAmount;
            }
            else
            {
                order.PaidAmount = method == PaymentMethod.Card ? order.TotalAmount : Math.Max(order.PaidAmount, order.TotalAmount);
                order.ChangeAmount = method == PaymentMethod.Card ? 0 : order.PaidAmount - order.TotalAmount;
            }
        }
    }

    /// <summary>
    /// Sets a line to <paramref name="newQuantity"/> (0 removes it) and brings the discount, the totals, the single payment and its lines in step.
    /// The order must carry at most one payment (the caller refuses a split receipt).
    /// </summary>
    public static void ApplyLineQuantity(Domain.Entities.Order order, OrderLine line, int newQuantity, IReadOnlyList<OrderPayment> payments)
    {
        line.Quantity = newQuantity;
        if (newQuantity == 0)
            line.Status = OrderLineStatus.Cancelled;

        var lines = OrderPaymentMath.ActiveLines(order);
        var subtotal = OrderPaymentMath.Subtotal(lines);

        // A fixed discount cannot exceed what is left to discount.
        order.DiscountAmount = Math.Min(order.DiscountAmount, subtotal);
        var bill = OrderPaymentMath.BillTotal(order, subtotal);
        var service = order.ServiceChargeAmount ?? 0;
        order.TotalAmount = bill + service;

        if (payments.Count == 1)
        {
            var payment = payments[0];
            var due = bill + service;
            payment.Amount = bill;
            payment.ReceivedAmount = payment.Method == PaymentMethod.Card ? due : Math.Max(payment.ReceivedAmount, due);
            payment.ChangeAmount = payment.Method == PaymentMethod.Card ? 0 : payment.ReceivedAmount - due;

            var factor = OrderPaymentMath.DiscountFactor(subtotal, order.DiscountAmount);
            var byId = order.Lines.DistinctBy(l => l.Id).ToDictionary(l => l.Id);
            foreach (var paymentLine in payment.Lines.ToList())
            {
                if (!byId.TryGetValue(paymentLine.OrderLineId, out var orderLine) || orderLine.Status == OrderLineStatus.Cancelled)
                {
                    payment.Lines.Remove(paymentLine);
                    continue;
                }
                if (paymentLine.OrderLineId == line.Id)
                    paymentLine.Quantity = newQuantity;
                paymentLine.Amount = OrderPaymentMath.Portion(orderLine, paymentLine.Quantity, factor);
            }

            // Whatever rounding leaves lands on the last line, so the lines add up to the payment.
            var lastLine = payment.Lines.LastOrDefault();
            if (lastLine is not null)
                lastLine.Amount += bill - payment.Lines.Sum(l => l.Amount);
        }

        if (payments.Count <= 1)
        {
            var received = payments.Count == 1 ? payments[0].ReceivedAmount : (order.PaymentMethod == PaymentMethod.Card ? order.TotalAmount : Math.Max(order.PaidAmount, order.TotalAmount));
            order.PaidAmount = received;
            order.ChangeAmount = order.PaymentMethod == PaymentMethod.Card ? 0 : received - order.TotalAmount;
        }
    }
}
