using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.SetServiceCharge;

public class SetOrderServiceChargeCommandHandler : IRequestHandler<SetOrderServiceChargeCommand, decimal>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public SetOrderServiceChargeCommandHandler(IOrderRepository orderRepository, ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<decimal> Handle(SetOrderServiceChargeCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, _currentUserService.CompanyId, cancellationToken)
            ?? throw new NotFoundException("Sifariş tapılmadı.");

        OrderGuards.EnsureNotBillLocked(order);

        if (order.IsPaid || order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new BadRequestException("Ödənilmiş və ya ləğv edilmiş sifarişə servis haqqı yazmaq olmaz.");

        var amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        order.ServiceChargeAmount = amount > 0 ? amount : null;

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return amount;
    }
}
