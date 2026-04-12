using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.OrderLines.Commands.Add;

public class AddOrderLineCommandHandler : IRequestHandler<AddOrderLineCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICurrentUserService _currentUserService;

    public AddOrderLineCommandHandler(
        IOrderRepository orderRepository,
        IOrderLineRepository orderLineRepository,
        IMenuItemRepository menuItemRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _orderLineRepository = orderLineRepository;
        _menuItemRepository = menuItemRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderResponse> Handle(AddOrderLineCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(
            request.Request.OrderId,
            companyId,
            cancellationToken);

        if (order is null)
            throw new Exception("Sifariş tapılmadı.");

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new Exception("Bu sifarişə məhsul əlavə etmək olmaz.");

        var menuItem = await _menuItemRepository.GetByIdAsync(
            request.Request.MenuItemId,
            companyId,
            cancellationToken);

        if (menuItem is null)
            throw new Exception("MenuItem tapılmadı.");

        var lineTotal = menuItem.Price * request.Request.Quantity;

        var orderLine = new OrderLine
        {
            CompanyId = companyId,
            OrderId = order.Id,
            MenuItemId = menuItem.Id,
            Quantity = request.Request.Quantity,
            UnitPrice = menuItem.Price,
            LineTotal = lineTotal,
            Note = request.Request.Note,
            Status = OrderLineStatus.Pending
        };

        await _orderLineRepository.AddAsync(orderLine, cancellationToken);

        order.TotalAmount += orderLine.LineTotal;

        if (order.Status == OrderStatus.Open)
            order.Status = OrderStatus.InPreparation;

        _orderRepository.Update(order);

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