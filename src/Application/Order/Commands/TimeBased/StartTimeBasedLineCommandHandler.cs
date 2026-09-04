using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.OrderLines.Commands.TimeBased;

public class StartTimeBasedLineCommandHandler : IRequestHandler<StartTimeBasedLineCommand, OrderResponse>
{
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public StartTimeBasedLineCommandHandler(
        IOrderLineRepository orderLineRepository,
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderLineRepository = orderLineRepository;
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderResponse> Handle(StartTimeBasedLineCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var line = await _orderLineRepository.GetByIdAsync(request.OrderLineId, companyId, cancellationToken);
        if (line is null)
            throw new Exception("Order line tapılmadı.");

        var order = await _orderRepository.GetByIdAsync(line.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Sifariş tapılmadı.");

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new Exception("Bu sifarişin line-ı dəyişdirilə bilməz.");

        if (!line.MenuItem.IsTimeBased)
            throw new Exception("Bu məhsul vaxta görə hesablanmır.");

        if (line.TimeBasedStartedAt is not null)
            throw new Exception("Taymer artıq başladılıb.");

        line.TimeBasedStartedAt = DateTime.UtcNow;
        line.TimeBasedStoppedAt = null;

        _orderLineRepository.Update(line);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        var updatedOrder = await _orderRepository.GetByIdAsync(order.Id, companyId, cancellationToken);
        if (updatedOrder is null)
            throw new Exception("Yenilənmiş sifariş tapılmadı.");

        return new OrderResponse
        {
            Id = updatedOrder.Id,
            CompanyId = updatedOrder.CompanyId,
            OrderNumber = updatedOrder.OrderNumber,
            RestaurantId = updatedOrder.RestaurantId,
            RestaurantName = updatedOrder.Restaurant?.Name,
            TableId = updatedOrder.TableId,
            TableName = updatedOrder.Table?.Name,
            WaiterId = updatedOrder.WaiterId,
            WaiterName = updatedOrder.Waiter != null
                ? $"{updatedOrder.Waiter.FirstName} {updatedOrder.Waiter.LastName}"
                : null,
            Status = updatedOrder.Status.ToString(),
            Note = updatedOrder.Note,
            GuestCount = updatedOrder.GuestCount,
            OpenedAt = updatedOrder.OpenedAt,
            ClosedAt = updatedOrder.ClosedAt,
            TotalAmount = updatedOrder.TotalAmount,
            DiscountCode = updatedOrder.DiscountCode,
            DiscountAmount = updatedOrder.DiscountAmount,
            Lines = updatedOrder.Lines.DistinctBy(x => x.Id).Select(x => new OrderLineResponse
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
                Note = x.Note,
                Status = x.Status.ToString(),
                ParentLineId = x.ParentLineId
            }).ToList()
        };
    }
}
