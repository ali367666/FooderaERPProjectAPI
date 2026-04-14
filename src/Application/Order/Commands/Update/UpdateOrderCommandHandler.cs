using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Orders.Commands.Update;

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateOrderCommandHandler> _logger;

    public UpdateOrderCommandHandler(
        IOrderRepository orderRepository,
        IRestaurantTableRepository restaurantTableRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<UpdateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _restaurantTableRepository = restaurantTableRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<OrderResponse> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "UpdateOrderCommand başladı. OrderId: {OrderId}, CompanyId: {CompanyId}",
            request.Request.Id,
            companyId);

        var order = await _orderRepository.GetByIdAsync(
            request.Request.Id,
            companyId,
            cancellationToken);

        if (order is null)
        {
            _logger.LogWarning(
                "Order update olunmadı. Sifariş tapılmadı. OrderId: {OrderId}",
                request.Request.Id);

            throw new Exception("Sifariş tapılmadı.");
        }

        var table = await _restaurantTableRepository.GetByIdAsync(
            request.Request.TableId,
            companyId,
            cancellationToken);

        if (table is null)
        {
            _logger.LogWarning(
                "Order update olunmadı. Masa tapılmadı. TableId: {TableId}",
                request.Request.TableId);

            throw new Exception("Masa tapılmadı.");
        }

        var waiter = await _employeeRepository.GetByIdAsync(
            request.Request.WaiterId,
            companyId,
            cancellationToken);

        if (waiter is null)
        {
            _logger.LogWarning(
                "Order update olunmadı. Garson tapılmadı. WaiterId: {WaiterId}",
                request.Request.WaiterId);

            throw new Exception("Garson tapılmadı.");
        }

        var oldValues = JsonSerializer.Serialize(new
        {
            order.Id,
            order.OrderNumber,
            order.RestaurantId,
            order.TableId,
            order.WaiterId,
            order.Status,
            order.Note,
            order.OpenedAt,
            order.ClosedAt,
            order.TotalAmount
        });

        if (!string.IsNullOrWhiteSpace(request.Request.Status))
        {
            if (!Enum.TryParse<OrderStatus>(request.Request.Status, true, out var parsedStatus))
            {
                _logger.LogWarning(
                    "Order update olunmadı. Status yanlışdır. OrderId: {OrderId}, Status: {Status}",
                    order.Id,
                    request.Request.Status);

                throw new Exception("Order status düzgün deyil.");
            }

            order.Status = parsedStatus;

            if (parsedStatus == OrderStatus.Paid || parsedStatus == OrderStatus.Cancelled)
                order.ClosedAt = DateTime.UtcNow;
            else
                order.ClosedAt = null;
        }

        order.RestaurantId = request.Request.RestaurantId;
        order.TableId = request.Request.TableId;
        order.WaiterId = request.Request.WaiterId;
        order.Note = string.IsNullOrWhiteSpace(request.Request.Note)
            ? null
            : request.Request.Note.Trim();

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        var updatedOrder = await _orderRepository.GetByIdAsync(
            order.Id,
            companyId,
            cancellationToken);

        if (updatedOrder is null)
        {
            _logger.LogWarning(
                "Yenilənmiş sifariş tapılmadı. OrderId: {OrderId}",
                order.Id);

            throw new Exception("Yenilənmiş sifariş tapılmadı.");
        }

        var newValues = JsonSerializer.Serialize(new
        {
            updatedOrder.Id,
            updatedOrder.OrderNumber,
            updatedOrder.RestaurantId,
            updatedOrder.TableId,
            updatedOrder.WaiterId,
            updatedOrder.Status,
            updatedOrder.Note,
            updatedOrder.OpenedAt,
            updatedOrder.ClosedAt,
            updatedOrder.TotalAmount
        });

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Order",
                    EntityId = updatedOrder.Id.ToString(),
                    ActionType = "Update",
                    OldValues = oldValues,
                    NewValues = newValues,
                    Message = $"Order yeniləndi. Id: {updatedOrder.Id}, OrderNumber: {updatedOrder.OrderNumber}, Status: {updatedOrder.Status}, TableId: {updatedOrder.TableId}, WaiterId: {updatedOrder.WaiterId}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "Order üçün audit log yazıldı. OrderId: {OrderId}",
                updatedOrder.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "Order update audit log yazılarkən xəta baş verdi. OrderId: {OrderId}",
                updatedOrder.Id);
        }

        _logger.LogInformation(
            "Order uğurla yeniləndi. OrderId: {OrderId}, CompanyId: {CompanyId}",
            updatedOrder.Id,
            companyId);

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