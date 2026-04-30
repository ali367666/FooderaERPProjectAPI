using System.Text.Json;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Kitchen.Commands.MarkReady;

public class MarkKitchenOrderLineReadyCommandHandler
    : IRequestHandler<MarkKitchenOrderLineReadyCommand>
{
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IRecipeStockDeductionService _recipeStockDeductionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<MarkKitchenOrderLineReadyCommandHandler> _logger;

    public MarkKitchenOrderLineReadyCommandHandler(
        IOrderLineRepository orderLineRepository,
        IOrderRepository orderRepository,
        IRecipeStockDeductionService recipeStockDeductionService,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<MarkKitchenOrderLineReadyCommandHandler> logger)
    {
        _orderLineRepository = orderLineRepository;
        _orderRepository = orderRepository;
        _recipeStockDeductionService = recipeStockDeductionService;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task Handle(
        MarkKitchenOrderLineReadyCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "MarkKitchenOrderLineReadyCommand başladı. OrderLineId: {OrderLineId}, CompanyId: {CompanyId}",
            request.OrderLineId,
            _currentUserService.CompanyId);

        var orderLine = await _orderLineRepository.GetByIdAsync(
            request.OrderLineId,
            _currentUserService.CompanyId,
            cancellationToken);

        if (orderLine is null)
        {
            _logger.LogWarning(
                "Kitchen ready əməliyyatı uğursuz oldu. OrderLine tapılmadı. OrderLineId: {OrderLineId}",
                request.OrderLineId);

            throw new NotFoundException("Kitchen order line was not found.");
        }

        if (orderLine.PreparationType != PreparationType.Kitchen)
        {
            _logger.LogWarning(
                "Kitchen ready əməliyyatı uğursuz oldu. Məhsul kitchen-ə aid deyil. OrderLineId: {OrderLineId}",
                request.OrderLineId);

            throw new BadRequestException("This order line is not a kitchen item.");
        }

        if (orderLine.Status != OrderLineStatus.InPreparation)
        {
            _logger.LogWarning(
                "Kitchen ready əməliyyatı uğursuz oldu. Status uyğun deyil. OrderLineId: {OrderLineId}, CurrentStatus: {Status}",
                request.OrderLineId,
                orderLine.Status);

            throw new BadRequestException("Only accepted kitchen lines can be marked as ready.");
        }

        if (!orderLine.IsStockDeducted)
        {
            await _recipeStockDeductionService.DeductForOrderLineAsync(orderLine, cancellationToken);
        }

        var oldOrderLineValues = JsonSerializer.Serialize(new
        {
            orderLine.Id,
            orderLine.OrderId,
            orderLine.MenuItemId,
            orderLine.Quantity,
            orderLine.PreparationType,
            orderLine.Status
        });

        orderLine.Status = OrderLineStatus.Ready;
        _orderLineRepository.Update(orderLine);

        string? oldOrderValues = null;
        string? newOrderValues = null;
        OrderStatus? oldOrderStatus = null;

        var order = await _orderRepository.GetByIdWithLinesAsync(
            orderLine.OrderId,
            _currentUserService.CompanyId,
            cancellationToken);

        if (order is not null)
        {
            oldOrderStatus = order.Status;

            oldOrderValues = JsonSerializer.Serialize(new
            {
                order.Id,
                order.Status
            });

            var activeKitchenLines = order.Lines
                .Where(x => x.PreparationType == PreparationType.Kitchen && x.Status != OrderLineStatus.Cancelled)
                .ToList();

            if (activeKitchenLines.Count > 0 &&
                activeKitchenLines.All(x => x.Status == OrderLineStatus.Ready || x.Status == OrderLineStatus.Served))
                order.Status = OrderStatus.Ready;
            else if (activeKitchenLines.Any(x => x.Status == OrderLineStatus.InPreparation))
                order.Status = OrderStatus.InPreparation;
            else
                order.Status = OrderStatus.Open;

            _orderRepository.Update(order);

            newOrderValues = JsonSerializer.Serialize(new
            {
                order.Id,
                order.Status
            });
        }

        await _orderLineRepository.SaveChangesAsync(cancellationToken);

        var newOrderLineValues = JsonSerializer.Serialize(new
        {
            orderLine.Id,
            orderLine.OrderId,
            orderLine.MenuItemId,
            orderLine.Quantity,
            orderLine.PreparationType,
            orderLine.Status
        });

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "OrderLine",
                    EntityId = orderLine.Id.ToString(),
                    ActionType = "MarkReady",
                    OldValues = oldOrderLineValues,
                    NewValues = newOrderLineValues,
                    Message = $"Kitchen OrderLine hazır oldu. OrderLineId: {orderLine.Id}, OrderId: {orderLine.OrderId}, YeniStatus: {orderLine.Status}",
                    IsSuccess = true
                },
                cancellationToken);

            if (order is not null && oldOrderStatus != order.Status)
            {
                await _auditLogService.LogAsync(
                    new AuditLogEntry
                    {
                        EntityName = "Order",
                        EntityId = order.Id.ToString(),
                        ActionType = "StatusUpdate",
                        OldValues = oldOrderValues,
                        NewValues = newOrderValues,
                        Message = $"Order status yeniləndi. OrderId: {order.Id}, KöhnəStatus: {oldOrderStatus}, YeniStatus: {order.Status}",
                        IsSuccess = true
                    },
                    cancellationToken);
            }

            _logger.LogInformation(
                "Kitchen ready audit log yazıldı. OrderLineId: {OrderLineId}",
                orderLine.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "Kitchen ready audit log yazılarkən xəta baş verdi. OrderLineId: {OrderLineId}",
                orderLine.Id);
        }

        _logger.LogInformation(
            "Kitchen OrderLine uğurla Ready oldu. OrderLineId: {OrderLineId}, OrderId: {OrderId}",
            orderLine.Id,
            orderLine.OrderId);
    }
}