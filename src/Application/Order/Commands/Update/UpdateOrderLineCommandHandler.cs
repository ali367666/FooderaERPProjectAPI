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

namespace Application.OrderLines.Commands.Update;

public class UpdateOrderLineCommandHandler : IRequestHandler<UpdateOrderLineCommand, OrderResponse>
{
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IRecipeStockDeductionService _recipeStockDeductionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateOrderLineCommandHandler> _logger;

    public UpdateOrderLineCommandHandler(
        IOrderLineRepository orderLineRepository,
        IOrderRepository orderRepository,
        IRecipeStockDeductionService recipeStockDeductionService,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<UpdateOrderLineCommandHandler> logger)
    {
        _orderLineRepository = orderLineRepository;
        _orderRepository = orderRepository;
        _recipeStockDeductionService = recipeStockDeductionService;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<OrderResponse> Handle(UpdateOrderLineCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "UpdateOrderLineCommand başladı. OrderLineId: {OrderLineId}, CompanyId: {CompanyId}",
            request.Request.Id,
            companyId);

        var line = await _orderLineRepository.GetByIdAsync(
            request.Request.Id,
            companyId,
            cancellationToken);

        if (line is null)
        {
            _logger.LogWarning(
                "OrderLine update olunmadı. Tapılmadı. OrderLineId: {OrderLineId}",
                request.Request.Id);

            throw new Exception("Order line tapılmadı.");
        }

        var order = await _orderRepository.GetByIdAsync(
            line.OrderId,
            companyId,
            cancellationToken);

        if (order is null)
        {
            _logger.LogWarning(
                "OrderLine update olunmadı. Sifariş tapılmadı. OrderId: {OrderId}",
                line.OrderId);

            throw new Exception("Sifariş tapılmadı.");
        }

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
        {
            _logger.LogWarning(
                "OrderLine update olunmadı. Sifariş statusu uyğun deyil. OrderId: {OrderId}, Status: {Status}",
                order.Id,
                order.Status);

            throw new Exception("Bu sifarişin line-ı dəyişdirilə bilməz.");
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

        var previousQuantity = line.Quantity;
        var newQuantity = request.Request.Quantity;
        var quantityChanged = newQuantity != previousQuantity;

        if (quantityChanged && line.IsStockDeducted)
        {
            await _recipeStockDeductionService.RestoreForOrderLineAsync(line, cancellationToken);
        }

        line.Quantity = newQuantity;
        line.Note = string.IsNullOrWhiteSpace(request.Request.Note)
            ? null
            : request.Request.Note.Trim();

        if (!string.IsNullOrWhiteSpace(request.Request.Status))
        {
            if (!Enum.TryParse<OrderLineStatus>(request.Request.Status, true, out var parsedStatus))
            {
                _logger.LogWarning(
                    "OrderLine update olunmadı. Status yanlışdır. OrderLineId: {OrderLineId}, Status: {Status}",
                    line.Id,
                    request.Request.Status);

                throw new Exception("Order line status düzgün deyil.");
            }

            line.Status = parsedStatus;
        }

        line.UnitPrice = request.Request.UnitPrice.HasValue && _currentUserService.HasPermission(Domain.Constants.AppPermissions.PosChangePrice)
            ? request.Request.UnitPrice.Value
            : line.MenuItem.StationPrice ?? line.MenuItem.Price;
        line.LineTotal = line.UnitPrice * line.Quantity;

        if (quantityChanged)
        {
            await _recipeStockDeductionService.DeductForOrderLineAsync(line, cancellationToken);
        }

        if (line.MenuItem.IsSet && line.MenuItem.SetComponents.Count > 0)
        {
            var oldChildren = order.Lines.Where(x => x.ParentLineId == line.Id).ToList();
            foreach (var oldChild in oldChildren)
            {
                await _recipeStockDeductionService.RestoreForOrderLineAsync(oldChild, cancellationToken);
                _orderLineRepository.Delete(oldChild);
                order.Lines.Remove(oldChild);
            }

            foreach (var component in line.MenuItem.SetComponents)
            {
                var childLine = new OrderLine
                {
                    OrderId = order.Id,
                    MenuItemId = component.ComponentMenuItemId,
                    Quantity = component.Quantity * line.Quantity,
                    UnitPrice = 0,
                    LineTotal = 0,
                    PreparationType = component.ComponentMenuItem.PreparationType,
                    Status = component.ComponentMenuItem.PreparationType == PreparationType.None
                        ? OrderLineStatus.Ready
                        : OrderLineStatus.Pending,
                    CompanyId = companyId,
                    ParentLine = line
                };
                await _orderLineRepository.AddAsync(childLine, cancellationToken);
                order.Lines.Add(childLine);
                await _recipeStockDeductionService.DeductForOrderLineAsync(childLine, cancellationToken);
            }
        }

        var subtotal = order.Lines
            .DistinctBy(x => x.Id)
            .Where(x => x.Status != OrderLineStatus.Cancelled)
            .Sum(x => x.Id == line.Id ? line.LineTotal : x.LineTotal);
        order.TotalAmount = Math.Max(0, subtotal - order.DiscountAmount);

        _orderLineRepository.Update(line);
        _orderRepository.Update(order);

        await _orderRepository.SaveChangesAsync(cancellationToken);

        var newOrderLineValues = JsonSerializer.Serialize(new
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
                    ActionType = "Update",
                    OldValues = oldOrderLineValues,
                    NewValues = newOrderLineValues,
                    Message = $"OrderLine yeniləndi. OrderLineId: {line.Id}, OrderId: {line.OrderId}, YeniSay: {line.Quantity}, YeniStatus: {line.Status}, YeniMəbləğ: {line.LineTotal}",
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
                    Message = $"Order yeniləndi. OrderId: {order.Id}, YeniTotalAmount: {order.TotalAmount}, Status: {order.Status}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "UpdateOrderLine audit logları yazıldı. OrderLineId: {OrderLineId}, OrderId: {OrderId}",
                line.Id,
                order.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "UpdateOrderLine audit log yazılarkən xəta baş verdi. OrderLineId: {OrderLineId}, OrderId: {OrderId}",
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
            "OrderLine uğurla yeniləndi. OrderLineId: {OrderLineId}, OrderId: {OrderId}",
            line.Id,
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
            GuestCount = updatedOrder.GuestCount,
            CounterpartyId = updatedOrder.CounterpartyId,
            CounterpartyName = updatedOrder.Counterparty?.Name,
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