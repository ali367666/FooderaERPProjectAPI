using Application.Common.Exceptions;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Orders.Dtos;
using Application.Orders.Dtos.Request;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.Pay;

public class PayOrderCommandHandler : IRequestHandler<PayOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRecipeStockDeductionService _recipeStockDeductionService;

    public PayOrderCommandHandler(
        IOrderRepository orderRepository,
        IRecipeStockDeductionService recipeStockDeductionService)
    {
        _orderRepository = orderRepository;
        _recipeStockDeductionService = recipeStockDeductionService;
    }

    public async Task<OrderResponse> Handle(PayOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithLinesAsync(request.OrderId, cancellationToken);
        if (order is null)
            throw new NotFoundException("Order not found.");

        if (order.IsPaid || order.Status == OrderStatus.Paid)
            throw new BadRequestException("This order is already paid.");

        if (order.Status != OrderStatus.Ready && order.Status != OrderStatus.Served)
            throw new BadRequestException("Only ready or served orders can be paid.");

        if (!Enum.TryParse<PaymentMethod>(request.Request.PaymentMethod, true, out var paymentMethod))
            throw new BadRequestException("Please select a valid payment method.");

        foreach (var line in order.Lines)
            line.LineTotal = line.UnitPrice * line.Quantity;

        var totalAmount = order.Lines
            .Where(x => x.Status != OrderLineStatus.Cancelled)
            .Sum(x => x.LineTotal);

        if (request.Request.PaidAmount < totalAmount)
            throw new BadRequestException("Paid amount cannot be less than total amount.");

        var nonKitchenLines = order.Lines
            .Where(x =>
                x.Status != OrderLineStatus.Cancelled &&
                (x.MenuItem?.PreparationType ?? x.PreparationType) != PreparationType.Kitchen)
            .ToList();
        foreach (var line in nonKitchenLines.Where(x => !x.IsStockDeducted))
        {
            await _recipeStockDeductionService.DeductForOrderLineAsync(line, cancellationToken);
        }

        order.TotalAmount = totalAmount;
        order.IsPaid = true;
        order.PaidAt = DateTime.UtcNow;
        order.PaymentMethod = paymentMethod;
        order.PaidAmount = request.Request.PaidAmount;
        order.ChangeAmount = request.Request.PaidAmount - totalAmount;
        order.Status = OrderStatus.Paid;
        order.ReceiptNumber = $"RCPT-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{order.Id}";
        order.ClosedAt = order.PaidAt;
        if (order.Table is not null)
            order.Table.IsOccupied = false;

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
            OpenedAt = order.OpenedAt,
            ClosedAt = order.ClosedAt,
            TotalAmount = order.TotalAmount,
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
                PreparationType = x.PreparationType,
                Status = x.Status.ToString(),
                Note = x.Note
            }).ToList()
        };
    }
}
