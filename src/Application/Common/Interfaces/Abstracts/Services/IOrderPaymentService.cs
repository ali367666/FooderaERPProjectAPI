using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Services;

public interface IOrderPaymentService
{
    /// <summary>
    /// Records one payment against the order and closes the order when this payment completes it.
    /// <paramref name="lines"/> null means "everything still unpaid" (the full-pay path of an order
    /// that already has part payments). <paramref name="settleAmount"/> pays "by amount" instead — a
    /// sum of money with no items attached (cash + card on one bill); it is capped at what is left.
    /// The service charge recorded on the order is added to the payment that closes it. Saves the changes.
    /// </summary>
    Task<PartPaymentResult> PayAsync(
        Domain.Entities.Order order,
        IReadOnlyList<PartPaymentLine>? lines,
        decimal? settleAmount,
        PaymentMethod method,
        decimal receivedAmount,
        bool isFiscal,
        CancellationToken cancellationToken);
}
