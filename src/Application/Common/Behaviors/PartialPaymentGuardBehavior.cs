using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Discounts.Commands.ApplyToOrder;
using Application.Discounts.Commands.RemoveFromOrder;
using Application.Discounts.Commands.SetManual;
using Application.OrderLines.Commands.Delete;
using Application.OrderLines.Commands.Update;
using Application.Orders.Commands.Delete;
using MediatR;

namespace Application.Common.Behaviors;

/// <summary>
/// Once some guests have paid their share ("Hesab"), what they paid for is settled: their items
/// cannot be removed or cheaper, the order cannot be deleted, and the order discount — which was
/// spread over their payments — cannot change.
/// </summary>
public class PartialPaymentGuardBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IOrderPaymentRepository _paymentRepository;
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly ICurrentUserService _currentUserService;

    public PartialPaymentGuardBehavior(
        IOrderPaymentRepository paymentRepository,
        IOrderLineRepository orderLineRepository,
        ICurrentUserService currentUserService)
    {
        _paymentRepository = paymentRepository;
        _orderLineRepository = orderLineRepository;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        switch (request)
        {
            case DeleteOrderLineCommand del:
                if (await PaidQuantityAsync(del.Id, cancellationToken) > 0)
                    throw new BadRequestException("Bu məhsul artıq ödənilib — silinə bilməz.");
                await EnsureNoAmountPaymentsAsync(del.Id, cancellationToken);
                break;

            case UpdateOrderLineCommand upd:
            {
                var paid = await PaidQuantityAsync(upd.Request.Id, cancellationToken);
                if (paid > 0
                    && (upd.Request.Quantity < paid
                        || upd.Request.UnitPrice is not null
                        || upd.Request.PriceType is not null
                        || upd.Request.IsGift is not null
                        || upd.Request.DiscountAmount is not null))
                    throw new BadRequestException(
                        "Ödənilmiş məhsulun miqdarı azaldıla, qiyməti və ya endirimi dəyişdirilə bilməz.");
                if (upd.Request.Quantity > 0
                    && (upd.Request.UnitPrice is not null || upd.Request.PriceType is not null
                        || upd.Request.IsGift is not null || upd.Request.DiscountAmount is not null))
                    await EnsureNoAmountPaymentsAsync(upd.Request.Id, cancellationToken);
                break;
            }

            case DeleteOrderCommand delOrder:
                await EnsureNoPaymentsAsync(delOrder.Id, "Qismən ödənilmiş sifariş silinə bilməz.", cancellationToken);
                break;

            case ApplyDiscountToOrderCommand apply:
                await EnsureNoPaymentsAsync(apply.OrderId, "Qismən ödənilmiş sifarişə endirim tətbiq etmək olmaz.", cancellationToken);
                break;

            case SetManualDiscountCommand manual:
                await EnsureNoPaymentsAsync(manual.OrderId, "Qismən ödənilmiş sifarişə endirim tətbiq etmək olmaz.", cancellationToken);
                break;

            case RemoveDiscountFromOrderCommand remove:
                await EnsureNoPaymentsAsync(remove.OrderId, "Qismən ödənilmiş sifarişdə endirimi dəyişmək olmaz.", cancellationToken);
                break;
        }

        return await next();
    }

    /// <summary>Part of the bill was paid as a plain sum — which items it covers is unknown, so the bill stays as it is.</summary>
    private async Task EnsureNoAmountPaymentsAsync(int lineId, CancellationToken cancellationToken)
    {
        var line = await _orderLineRepository.GetByIdAsync(lineId, _currentUserService.CompanyId, cancellationToken);
        if (line is not null && await _paymentRepository.AnyByAmountAsync(line.OrderId, cancellationToken))
            throw new BadRequestException("Hesabın bir hissəsi ödənilib — məhsulu silmək və ya qiymətini dəyişmək olmaz.");
    }

    private async Task<int> PaidQuantityAsync(int lineId, CancellationToken cancellationToken)
    {
        var line = await _orderLineRepository.GetByIdAsync(lineId, _currentUserService.CompanyId, cancellationToken);
        return line is null ? 0 : await _paymentRepository.GetPaidQuantityAsync(line.Id, cancellationToken);
    }

    private async Task EnsureNoPaymentsAsync(int orderId, string message, CancellationToken cancellationToken)
    {
        if (await _paymentRepository.AnyForOrderAsync(orderId, cancellationToken))
            throw new BadRequestException(message);
    }
}
