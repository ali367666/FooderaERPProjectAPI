using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.MoveTable;

public class MoveOrderTableCommandHandler : IRequestHandler<MoveOrderTableCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantTableRepository _tableRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;

    public MoveOrderTableCommandHandler(
        IOrderRepository orderRepository,
        IRestaurantTableRepository tableRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService)
    {
        _orderRepository = orderRepository;
        _tableRepository = tableRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
    }

    public async Task<OrderResponse> Handle(MoveOrderTableCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(request.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Order not found.");

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new Exception("This order can no longer be moved.");

        if (order.TableId == request.NewTableId)
            throw new Exception("This order is already on that table.");

        var newTable = await _tableRepository.GetByIdAsync(request.NewTableId, companyId, cancellationToken);
        if (newTable is null || newTable.RestaurantId != order.RestaurantId)
            throw new Exception("Table not found for this restaurant.");

        if (newTable.IsOccupied)
            throw new Exception("The selected table is already occupied.");

        var oldTable = await _tableRepository.GetByIdAsync(order.TableId, companyId, cancellationToken);
        var oldTableId = order.TableId;

        order.TableId = newTable.Id;
        newTable.IsOccupied = true;
        _tableRepository.Update(newTable);

        if (oldTable is not null)
        {
            oldTable.IsOccupied = false;
            _tableRepository.Update(oldTable);
        }

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Order",
                    EntityId = order.Id.ToString(),
                    ActionType = "MoveTable",
                    Message = $"Order {order.Id} masası dəyişdirildi: {oldTableId} -> {newTable.Id}",
                    IsSuccess = true
                },
                cancellationToken);
        }
        catch
        {
            // audit log failures must not block the operation
        }

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
            WaiterName = updatedOrder.Waiter != null ? $"{updatedOrder.Waiter.FirstName} {updatedOrder.Waiter.LastName}" : null,
            Status = updatedOrder.Status.ToString(),
            Note = updatedOrder.Note,
            GuestCount = updatedOrder.GuestCount,
            OpenedAt = updatedOrder.OpenedAt,
            ClosedAt = updatedOrder.ClosedAt,
            TotalAmount = updatedOrder.TotalAmount,
            DiscountCode = updatedOrder.DiscountCode,
            DiscountAmount = updatedOrder.DiscountAmount,
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
