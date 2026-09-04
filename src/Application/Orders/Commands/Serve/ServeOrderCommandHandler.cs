using Application.Common.Exceptions;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.Serve;

public class ServeOrderCommandHandler : IRequestHandler<ServeOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;

    public ServeOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderResponse> Handle(ServeOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithLinesAsync(request.OrderId, cancellationToken);
        if (order is null)
            throw new NotFoundException("Order not found.");

        if (order.Status != OrderStatus.Ready)
            throw new BadRequestException("Only ready orders can be served.");

        order.Status = OrderStatus.Served;
        foreach (var line in order.Lines.Where(x => x.Status == OrderLineStatus.Ready))
            line.Status = OrderLineStatus.Served;

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return new OrderResponse
        {
            Id = order.Id,
            CompanyId = order.CompanyId,
            OrderNumber = order.OrderNumber,
            RestaurantId = order.RestaurantId,
            RestaurantName = order.Restaurant?.Name,
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
            TotalAmount = order.TotalAmount,
            DiscountCode = order.DiscountCode,
            DiscountAmount = order.DiscountAmount,
            IsPaid = order.IsPaid,
            PaidAt = order.PaidAt,
            PaymentMethod = order.PaymentMethod?.ToString(),
            PaidAmount = order.PaidAmount,
            ChangeAmount = order.ChangeAmount,
            ReceiptNumber = order.ReceiptNumber,
            Lines = order.Lines.Select(x => new OrderLineResponse
            {
                Id = x.Id,
                MenuItemId = x.MenuItemId,
                MenuItemName = x.MenuItem.Name,
                MenuItemType = x.MenuItem.PreparationType.ToString(),
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal,
                HoldUntilUtc = x.HoldUntilUtc,
                KitchenPrintedAt = x.KitchenPrintedAt,
                TimeBasedStartedAt = x.TimeBasedStartedAt,
                TimeBasedStoppedAt = x.TimeBasedStoppedAt,
                IsTimeBased = x.MenuItem.IsTimeBased,
                PreparationType = x.PreparationType,
                Status = x.Status.ToString(),
                Note = x.Note
            }).ToList()
        };
    }
}
