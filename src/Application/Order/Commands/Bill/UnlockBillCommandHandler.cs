using System.Text;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.Bill;

public class UnlockBillCommandHandler : IRequestHandler<UnlockBillCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public UnlockBillCommandHandler(IOrderRepository orderRepository, ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UnlockBillCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, _currentUserService.CompanyId, cancellationToken)
            ?? throw new NotFoundException("Sifariş tapılmadı.");

        if (!order.IsBillLocked)
            return;

        order.IsBillLocked = false;
        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);
    }
}
