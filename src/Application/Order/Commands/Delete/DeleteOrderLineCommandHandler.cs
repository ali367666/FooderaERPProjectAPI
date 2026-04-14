using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.OrderLines.Commands.Delete;

public class DeleteOrderLineCommandHandler : IRequestHandler<DeleteOrderLineCommand, OrderResponse>
{
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteOrderLineCommandHandler> _logger;

    public DeleteOrderLineCommandHandler(
        IOrderLineRepository orderLineRepository,
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<DeleteOrderLineCommandHandler> logger)
    {
        _orderLineRepository = orderLineRepository;
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<OrderResponse> Handle(DeleteOrderLineCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "DeleteOrderLineCommand başladı. OrderLineId: {OrderLineId}, CompanyId: {CompanyId}",
            request.Id,
            companyId);

        var line = await _orderLineRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (line is null)
        {
            _logger.LogWarning(
                "OrderLine silinmədi. Tapılmadı. OrderLineId: {OrderLineId}",
                request.Id);

            throw new Exception("Order line tapılmadı.");
        }

        var order = await _orderRepository.GetByIdAsync(
            line.OrderId,
            companyId,
            cancellationToken);

        if (order is null)
        {
            _logger.LogWarning(
                "OrderLine silinmədi. Sifariş tapılmadı. OrderId: {OrderId}",
                line.OrderId);

            throw new Exception("Sifariş tapılmadı.");
        }

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
        {
            _logger.LogWarning(
                "OrderLine silinmədi. Sifariş statusu uyğun deyil. OrderId: {OrderId}, Status: {Status}",
                order.Id,
                order.Status);

            throw new Exception("Bu sifarişin line-ı silinə bilməz.");
        }

        var oldOrderLineValues = JsonSerializer.Serialize(new
        {
            line.Id,
            line.OrderId,
            line.MenuItemId,
            line.Quantity,
            line.UnitPrice,
            line.LineTotal,
            line.Note,
            line.PreparationType,
            line.Status
        });

        var oldOrderValues = JsonSerializer.Serialize(new
        {
            order.Id,
            order.Status,
            order.TotalAmount
        });

        if (line.Status != OrderLineStatus.Cancelled)
        {
            order.TotalAmount -= line.LineTotal;

            if (order.TotalAmount < 0)
                order.TotalAmount = 0;
        }

        _orderRepository.Update(order);
        _orderLineRepository.Delete(line);

        await _orderRepository.SaveChangesAsync(cancellationToken);

        var newOrderValues = JsonSerializer.Serialize(new
        {
            order.Id,
            order.Status,
            order.TotalAmount
        });

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "OrderLine",
                    EntityId = line.Id.ToString(),
                    ActionType = "Delete",
                    OldValues = oldOrderLineValues,
                    NewValues = null,
                    Message = $"OrderLine silindi. OrderLineId: {line.Id}, OrderId: {line.OrderId}, MenuItemId: {line.MenuItemId}, Məbləğ: {line.LineTotal}",
                    IsSuccess = true
                },
                cancellationToken);

            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Order",
                    EntityId = order.Id.ToString(),
                    ActionType = "Update",
                    OldValues = oldOrderValues,
                    NewValues = newOrderValues,
                    Message = $"Order total yeniləndi. OrderId: {order.Id}, YeniTotalAmount: {order.TotalAmount}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "DeleteOrderLine audit logları yazıldı. OrderLineId: {OrderLineId}, OrderId: {OrderId}",
                line.Id,
                order.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "DeleteOrderLine audit log yazılarkən xəta baş verdi. OrderLineId: {OrderLineId}, OrderId: {OrderId}",
                line.Id,
                order.Id);
        }

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

        _logger.LogInformation(
            "OrderLine uğurla silindi. OrderLineId: {OrderLineId}, OrderId: {OrderId}",
            line.Id,
            order.Id);

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