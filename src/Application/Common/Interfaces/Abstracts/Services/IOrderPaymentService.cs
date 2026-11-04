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
    /// With <paramref name="remainderMethod"/> (Cash or Card, the other one) a cash / card amount smaller than what is due pays only
    /// that part and the rest goes to the remainder method in a second payment ("10 in cash, 2 to the card").
    /// </summary>
    Task<PartPaymentResult> PayAsync(
        Domain.Entities.Order order,
        IReadOnlyList<PartPaymentLine>? lines,
        decimal? settleAmount,
        PaymentMethod method,
        decimal receivedAmount,
        bool isFiscal,
        int? bankId,
        CancellationToken cancellationToken,
        PaymentMethod? remainderMethod = null,
        int? remainderBankId = null);
}
