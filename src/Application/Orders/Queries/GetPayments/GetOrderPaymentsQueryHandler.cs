using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Queries.GetPayments;

public class GetOrderPaymentsQueryHandler : IRequestHandler<GetOrderPaymentsQuery, OrderPaymentsResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderPaymentRepository _paymentRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetOrderPaymentsQueryHandler(
        IOrderRepository orderRepository,
        IOrderPaymentRepository paymentRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderPaymentsResponse> Handle(GetOrderPaymentsQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithLinesAsync(request.OrderId, _currentUserService.CompanyId, cancellationToken);
        if (order is null)
            throw new NotFoundException("Order not found.");

        var payments = await _paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        var lines = OrderPaymentMath.ActiveLines(order);
        var subtotal = OrderPaymentMath.Subtotal(lines);
        var paidAmount = payments.Sum(p => p.Amount);

        return new OrderPaymentsResponse
        {
            PaidAmount = paidAmount,
            RemainingAmount = order.IsPaid ? 0 : Math.Max(0, OrderPaymentMath.BillTotal(order, subtotal) - paidAmount),
            DiscountFactor = OrderPaymentMath.DiscountFactor(subtotal, order.DiscountAmount),
            PaidLines = OrderPaymentMath.PaidQuantities(payments)
                .Select(kv => new PaidLineResponse { OrderLineId = kv.Key, PaidQuantity = kv.Value })
                .ToList(),
            Payments = payments.Select(p => new OrderPaymentResponse
            {
                Id = p.Id,
                Method = p.Method.ToString(),
                Amount = p.Amount,
                ServiceChargeAmount = p.ServiceChargeAmount ?? 0,
                ReceivedAmount = p.ReceivedAmount,
                ChangeAmount = p.ChangeAmount,
                PaidAtUtc = p.PaidAtUtc,
                Lines = p.Lines.Select(l => new OrderPaymentLineResponse
                {
                    OrderLineId = l.OrderLineId,
                    MenuItemName = l.OrderLine.MenuItem.Name,
                    Quantity = l.Quantity,
                    Amount = l.Amount
                }).ToList()
            }).ToList()
        };
    }
}
