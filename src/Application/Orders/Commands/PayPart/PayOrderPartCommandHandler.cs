using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.PayPart;

public class PayOrderPartCommandHandler : IRequestHandler<PayOrderPartCommand, PartPaymentResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderPaymentService _paymentService;
    private readonly ICurrentUserService _currentUserService;

    public PayOrderPartCommandHandler(
        IOrderRepository orderRepository,
        IOrderPaymentService paymentService,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _paymentService = paymentService;
        _currentUserService = currentUserService;
    }

    public async Task<PartPaymentResponse> Handle(PayOrderPartCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithLinesAsync(request.OrderId, cancellationToken);
        if (order is null || !_currentUserService.CanAccessCompany(order.CompanyId))
            throw new NotFoundException("Order not found.");

        if (!Enum.TryParse<PaymentMethod>(request.Request.PaymentMethod, true, out var method))
            throw new BadRequestException("Please select a valid payment method.");

        PaymentMethod? remainderMethod = null;
        if (!string.IsNullOrWhiteSpace(request.Request.RemainderPaymentMethod))
        {
            if (!Enum.TryParse<PaymentMethod>(request.Request.RemainderPaymentMethod, true, out var parsedRemainder))
                throw new BadRequestException("Qalan məbləğ üçün ödəniş üsulu düzgün deyil.");
            remainderMethod = parsedRemainder;
        }

        var result = await _paymentService.PayAsync(
            order,
            request.Request.Lines.Select(l => new PartPaymentLine(l.OrderLineId, l.Quantity)).ToList(),
            request.Request.Amount,
            method,
            request.Request.PaidAmount,
            request.Request.IsFiscal,
            request.Request.BankId,
            cancellationToken,
            remainderMethod,
            request.Request.RemainderBankId);

        var payments = new[] { result.Payment }.Concat(result.RemainderPayments ?? Array.Empty<Domain.Entities.OrderPayment>()).ToList();
        return new PartPaymentResponse
        {
            PaymentId = result.Payment.Id,
            Amount = payments.Sum(p => p.Amount),
            ChangeAmount = result.Payment.ChangeAmount,
            OrderClosed = result.OrderClosed,
            RemainingAmount = result.RemainingAmount,
            Parts = payments
                .Select(p => new PartPaymentPart { Method = p.Method.ToString(), Amount = p.Amount + (p.ServiceChargeAmount ?? 0) })
                .ToList()
        };
    }
}
