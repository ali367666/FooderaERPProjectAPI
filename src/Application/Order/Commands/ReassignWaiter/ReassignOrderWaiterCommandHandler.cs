using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.ReassignWaiter;

public class ReassignOrderWaiterCommandHandler : IRequestHandler<ReassignOrderWaiterCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;

    public ReassignOrderWaiterCommandHandler(
        IOrderRepository orderRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService)
    {
        _orderRepository = orderRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
    }

    public async Task<OrderResponse> Handle(ReassignOrderWaiterCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(request.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Order not found.");

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new Exception("This order can no longer be reassigned.");

        var newEmployee = await _employeeRepository.GetByIdAsync(request.NewEmployeeId, companyId, cancellationToken);
        if (newEmployee is null)
            throw new Exception("Employee not found.");

        var oldWaiterId = order.WaiterId;
        order.WaiterId = newEmployee.Id;

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Order",
                    EntityId = order.Id.ToString(),
                    ActionType = "ReassignWaiter",
                    Message = $"Order {order.Id} ofisiantı dəyişdirildi: {oldWaiterId} -> {newEmployee.Id}",
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
                HoldUntilUtc = x.HoldUntilUtc,
                Note = x.Note,
                Status = x.Status.ToString()
            }).ToList()
        };
    }
}
