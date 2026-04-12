using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.Orders.Commands.Delete;

public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, string>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteOrderCommandHandler(
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<string> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Sifariş tapılmadı.");

        _orderRepository.Delete(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return "Sifariş uğurla silindi.";
    }
}