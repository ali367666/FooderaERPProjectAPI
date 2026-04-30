using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Orders.Dtos;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.OrderLines.Commands.Add;

public class AddOrderLineCommandHandler : IRequestHandler<AddOrderLineCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IRecipeStockDeductionService _recipeStockDeductionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AddOrderLineCommandHandler> _logger;

    public AddOrderLineCommandHandler(
        IOrderRepository orderRepository,
        IOrderLineRepository orderLineRepository,
        IMenuItemRepository menuItemRepository,
        IRecipeStockDeductionService recipeStockDeductionService,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<AddOrderLineCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _orderLineRepository = orderLineRepository;
        _menuItemRepository = menuItemRepository;
        _recipeStockDeductionService = recipeStockDeductionService;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<OrderResponse> Handle(AddOrderLineCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "AddOrderLineCommand başladı. OrderId: {OrderId}, MenuItemId: {MenuItemId}, CompanyId: {CompanyId}",
            request.Request.OrderId,
            request.Request.MenuItemId,
            companyId);

        var order = await _orderRepository.GetByIdAsync(
            request.Request.OrderId,
            companyId,
            cancellationToken);

        if (order is null)
        {
            _logger.LogWarning(
                "OrderLine əlavə olunmadı. Sifariş tapılmadı. OrderId: {OrderId}",
                request.Request.OrderId);

            throw new Exception("Sifariş tapılmadı.");
        }

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
        {
            _logger.LogWarning(
                "OrderLine əlavə olunmadı. Sifarişə məhsul əlavə etmək olmaz. OrderId: {OrderId}, Status: {Status}",
                order.Id,
                order.Status);

            throw new Exception("Bu sifarişə məhsul əlavə etmək olmaz.");
        }

        var menuItem = await _menuItemRepository.GetByIdAsync(
            request.Request.MenuItemId,
            companyId,
            cancellationToken);

        if (menuItem is null)
        {
            _logger.LogWarning(
                "OrderLine əlavə olunmadı. MenuItem tapılmadı. MenuItemId: {MenuItemId}",
                request.Request.MenuItemId);

            throw new Exception("MenuItem tapılmadı.");
        }

        var oldOrderValues = JsonSerializer.Serialize(new
        {
            order.Id,
            order.Status,
            order.TotalAmount
        });

        var orderLine = new OrderLine
        {
            OrderId = order.Id,
            MenuItemId = menuItem.Id,
            Quantity = request.Request.Quantity,
            UnitPrice = menuItem.Price,
            LineTotal = menuItem.Price * request.Request.Quantity,
            Note = request.Request.Note,
            PreparationType = menuItem.PreparationType,
            Status = menuItem.PreparationType == PreparationType.None
                ? OrderLineStatus.Ready
                : OrderLineStatus.Pending,
            CompanyId = companyId
        };

        await _orderLineRepository.AddAsync(orderLine, cancellationToken);
        order.Lines.Add(orderLine);
        await _recipeStockDeductionService.DeductForOrderLineAsync(orderLine, cancellationToken);

        order.TotalAmount = order.Lines
            .Where(x => x.Status != OrderLineStatus.Cancelled)
            .Sum(x => x.LineTotal);

        _orderRepository.Update(order);

        await _orderRepository.SaveChangesAsync(cancellationToken);

        var newOrderLineValues = JsonSerializer.Serialize(new
        {
            orderLine.Id,
            orderLine.OrderId,
            orderLine.MenuItemId,
            orderLine.Quantity,
            orderLine.UnitPrice,
            orderLine.LineTotal,
            orderLine.Note,
            orderLine.PreparationType,
            orderLine.Status
        });

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
                    EntityId = orderLine.Id.ToString(),
                    ActionType = "Create",
                    NewValues = newOrderLineValues,
                    Message = $"OrderLine əlavə edildi. OrderLineId: {orderLine.Id}, OrderId: {orderLine.OrderId}, MenuItem: {menuItem.Name}, Say: {orderLine.Quantity}, PreparationType: {orderLine.PreparationType}, Status: {orderLine.Status}",
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
                    Message = $"Order yeniləndi. OrderId: {order.Id}, TotalAmount: {order.TotalAmount}, Status: {order.Status}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "AddOrderLine audit logları yazıldı. OrderLineId: {OrderLineId}, OrderId: {OrderId}",
                orderLine.Id,
                order.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "AddOrderLine audit log yazılarkən xəta baş verdi. OrderLineId: {OrderLineId}, OrderId: {OrderId}",
                orderLine.Id,
                order.Id);
        }

        var updatedOrder = await _orderRepository.GetByIdAsync(order.Id, companyId, cancellationToken);

        if (updatedOrder is null)
        {
            _logger.LogWarning(
                "Yenilənmiş sifariş tapılmadı. OrderId: {OrderId}",
                order.Id);

            throw new Exception("Yenilənmiş sifariş tapılmadı.");
        }

        _logger.LogInformation(
            "OrderLine uğurla əlavə olundu. OrderLineId: {OrderLineId}, OrderId: {OrderId}",
            orderLine.Id,
            order.Id);

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
            OpenedAt = updatedOrder.OpenedAt,
            ClosedAt = updatedOrder.ClosedAt,
            TotalAmount = updatedOrder.TotalAmount,
            Lines = updatedOrder.Lines.Select(x => new OrderLineResponse
            {
                Id = x.Id,
                MenuItemId = x.MenuItemId,
                MenuItemName = x.MenuItem.Name,
                MenuItemType = x.MenuItem.PreparationType.ToString(),
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal,
                PreparationType = x.PreparationType,
                Note = x.Note,
                Status = x.Status.ToString()
            }).ToList()
        };
    }
}