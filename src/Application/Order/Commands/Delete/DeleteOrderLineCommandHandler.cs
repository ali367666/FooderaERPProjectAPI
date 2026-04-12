using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.OrderLines.Commands.Delete;

public class DeleteOrderLineCommandHandler : IRequestHandler<DeleteOrderLineCommand, OrderResponse>
{
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteOrderLineCommandHandler(
        IOrderLineRepository orderLineRepository,
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderLineRepository = orderLineRepository;
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderResponse> Handle(DeleteOrderLineCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var line = await _orderLineRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (line is null)
            throw new Exception("Order line tapılmadı.");

        var order = await _orderRepository.GetByIdAsync(line.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Sifariş tapılmadı.");

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new Exception("Bu sifarişin line-ı silinə bilməz.");

        if (line.Status != OrderLineStatus.Cancelled)
        {
            order.TotalAmount -= line.LineTotal;

            if (order.TotalAmount < 0)
                order.TotalAmount = 0;
        }

        _orderRepository.Update(order);
        _orderLineRepository.Delete(line);

        await _orderRepository.SaveChangesAsync(cancellationToken);

        var updatedOrder = await _orderRepository.GetByIdAsync(order.Id, companyId, cancellationToken);
        if (updatedOrder is null)
            throw new Exception("Yenilənmiş sifariş tapılmadı.");

        return new OrderResponse
        {
            Id = updatedOrder.Id,
            OrderNumber = updatedOrder.OrderNumber,
            RestaurantId = updatedOrder.RestaurantId,
            TableId = updatedOrder.TableId,
            TableName = updatedOrder.Table?.Name,
            WaiterId = updatedOrder.WaiterId,
            WaiterName = updatedOrder.Waiter != null
                ? $"{updatedOrder.Waiter.FirstName} {updatedOrder.Waiter.LastName}"
                : null,
            Status = updatedOrder.Status.ToString(),
            Note = updatedOrder.Note,
            OpenedAt = updatedOrder.OpenedAt,
            ClosedAt = updatedOrder.ClosedAt,
            TotalAmount = updatedOrder.TotalAmount,
            Lines = updatedOrder.Lines.Select(x => new OrderLineResponse
            {
                Id = x.Id,
                MenuItemId = x.MenuItemId,
                MenuItemName = x.MenuItem.Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal,
                Note = x.Note,
                Status = x.Status.ToString()
            }).ToList()
        };
    }
}