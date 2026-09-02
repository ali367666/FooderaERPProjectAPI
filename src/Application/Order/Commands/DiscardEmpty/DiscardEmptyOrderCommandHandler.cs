using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.DiscardEmpty;

/// <summary>
/// Lets any waiter who can open a table quietly undo an accidental open — a Draft order
/// with zero lines was never really "placed", so this doesn't need the Pos.DeleteOrder
/// permission that real order cancellation requires.
/// </summary>
public class DiscardEmptyOrderCommandHandler : IRequestHandler<DiscardEmptyOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public DiscardEmptyOrderCommandHandler(
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DiscardEmptyOrderCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(request.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Sifariş tapılmadı.");

        if (order.Status != OrderStatus.Draft || order.Lines.Any(x => x.Status != OrderLineStatus.Cancelled))
            throw new Exception("Yalnız boş, hələ başlanmamış sifariş bu yolla ləğv edilə bilər.");

        order.Status = OrderStatus.Cancelled;
        order.ClosedAt = DateTime.UtcNow;

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);
    }
}
