using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.Start;

public class StartOrderCommandHandler : IRequestHandler<StartOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public StartOrderCommandHandler(
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderResponse> Handle(StartOrderCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var userId = _currentUserService.UserId;

        if (userId <= 0)
            throw new Exception("User is not authenticated.");

        var order = await _orderRepository.GetByIdAsync(request.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Order not found.");

        if (order.Status != OrderStatus.Open)
            throw new Exception("Only submitted orders can be started.");

        if (order.ProcessedByUserId.HasValue && order.ProcessedByUserId != userId)
            throw new Exception("This order is already assigned to another user.");

        order.Status = OrderStatus.InPreparation;
        order.ProcessedByUserId = userId;
        order.ProcessedAt = DateTime.UtcNow;

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        var updatedOrder = await _orderRepository.GetByIdAsync(order.Id, companyId, cancellationToken);
        if (updatedOrder is null)
            throw new Exception("Updated order not found.");

        return MapResponse(updatedOrder);
    }

    private static OrderResponse MapResponse(Domain.Entities.Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            RestaurantId = order.RestaurantId,
            TableId = order.TableId,
            TableName = order.Table?.Name,
            WaiterId = order.WaiterId,
            WaiterName = order.Waiter != null ? $"{order.Waiter.FirstName} {order.Waiter.LastName}" : null,
            ProcessedByUserId = order.ProcessedByUserId,
            ProcessedByUserName = order.ProcessedByUser?.FullName,
            ProcessedAt = order.ProcessedAt,
            Status = order.Status.ToString(),
            Note = order.Note,
            GuestCount = order.GuestCount,
            CounterpartyId = order.CounterpartyId,
            CounterpartyName = order.Counterparty?.Name,
            OpenedAt = order.OpenedAt,
            ClosedAt = order.ClosedAt,
            TotalAmount = Math.Max(0, order.Lines
                .DistinctBy(x => x.Id)
                .Where(x => x.Status != OrderLineStatus.Cancelled)
                .Sum(x => x.UnitPrice * x.Quantity) - order.DiscountAmount),
            DiscountCode = order.DiscountCode,
            DiscountAmount = order.DiscountAmount,
            Lines = order.Lines.DistinctBy(x => x.Id).Select(x => new OrderLineResponse
            {
                Id = x.Id,
                MenuItemId = x.MenuItemId,
                MenuItemName = x.MenuItem.Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal,
                HoldUntilUtc = x.HoldUntilUtc,
                KitchenPrintedAt = x.KitchenPrintedAt,
                TimeBasedStartedAt = x.TimeBasedStartedAt,
                TimeBasedStoppedAt = x.TimeBasedStoppedAt,
                IsTimeBased = x.MenuItem.IsTimeBased,
                Note = x.Note,
                Status = x.Status.ToString()
            }).ToList()
        };
    }
}
