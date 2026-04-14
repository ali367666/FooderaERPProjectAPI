using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.Kitchen.Commands.StartPreparation;

public class StartKitchenOrderLineCommandHandler
    : IRequestHandler<StartKitchenOrderLineCommand>
{
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public StartKitchenOrderLineCommandHandler(
        IOrderLineRepository orderLineRepository,
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderLineRepository = orderLineRepository;
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        StartKitchenOrderLineCommand request,
        CancellationToken cancellationToken)
    {
        var orderLine = await _orderLineRepository.GetByIdAsync(
            request.OrderLineId,
            _currentUserService.CompanyId,
            cancellationToken);

        if (orderLine is null)
            throw new Exception("OrderLine tapılmadı.");

        if (orderLine.PreparationType != PreparationType.Kitchen)
            throw new Exception("Bu məhsul mətbəxə aid deyil.");

        if (orderLine.Status != OrderLineStatus.Pending)
            throw new Exception("Yalnız Pending olan OrderLine hazırlanmağa başlaya bilər.");

        orderLine.Status = OrderLineStatus.InPreparation;
        _orderLineRepository.Update(orderLine);

        var order = await _orderRepository.GetByIdAsync(
            orderLine.OrderId,
            _currentUserService.CompanyId,
            cancellationToken);

        if (order is not null)
        {
            order.Status = OrderStatus.InPreparation;
            _orderRepository.Update(order);
        }

        await _orderLineRepository.SaveChangesAsync(cancellationToken);
    }
}