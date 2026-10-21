namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IOrderPaymentRepository
{
    Task AddAsync(Domain.Entities.OrderPayment payment, CancellationToken cancellationToken);

    /// <summary>The order's payments, oldest first, with their lines (and each line's menu item).</summary>
    Task<List<Domain.Entities.OrderPayment>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken);

    Task<Domain.Entities.OrderPayment?> GetByIdAsync(int paymentId, int orderId, CancellationToken cancellationToken);

    /// <summary>
    /// Card income of the given paid orders grouped by bank: the card parts of orders paid in parts
    /// plus orders paid by card in one go. Payments without a bank (made before banks existed) are
    /// grouped as "Bank göstərilməyib".
    /// </summary>
    Task<List<Application.Shift.Dtos.ZReportBankBreakdown>> GetCardBankTotalsAsync(
        IReadOnlyCollection<int> orderIds,
        CancellationToken cancellationToken);

    Task<bool> AnyForOrderAsync(int orderId, CancellationToken cancellationToken);

    /// <summary>True when part of the bill was settled by amount (no items attached).</summary>
    Task<bool> AnyByAmountAsync(int orderId, CancellationToken cancellationToken);

    /// <summary>How much of a line has already been paid for (the line's own quantity unit).</summary>
    Task<int> GetPaidQuantityAsync(int orderLineId, CancellationToken cancellationToken);
}
