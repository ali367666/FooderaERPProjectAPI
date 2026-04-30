using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.Submit;

public class SubmitOrderCommandHandler : IRequestHandler<SubmitOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public SubmitOrderCommandHandler(
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderResponse> Handle(SubmitOrderCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(request.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Order not found.");

        if (order.Status != OrderStatus.Draft)
            throw new Exception("Only draft orders can be submitted.");

        if (order.Lines.Count == 0)
            throw new Exception("Add at least one order line before submitting.");

        order.Status = OrderStatus.Open;
        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        var updatedOrder = await _orderRepository.GetByIdAsync(order.Id, companyId, cancellationToken);
        if (updatedOrder is null)
            throw new Exception("Updated order not found.");

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
            ProcessedByUserId = updatedOrder.ProcessedByUserId,
            ProcessedByUserName = updatedOrder.ProcessedByUser?.FullName,
            ProcessedAt = updatedOrder.ProcessedAt,
            Status = updatedOrder.Status.ToString(),
            Note = updatedOrder.Note,
            OpenedAt = updatedOrder.OpenedAt,
            ClosedAt = updatedOrder.ClosedAt,
            TotalAmount = updatedOrder.Lines
                .Where(x => x.Status != OrderLineStatus.Cancelled)
                .Sum(x => x.UnitPrice * x.Quantity),
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
