using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.OrderLines.Commands.Update;

public class UpdateOrderLineCommandHandler : IRequestHandler<UpdateOrderLineCommand, OrderResponse>
{
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateOrderLineCommandHandler(
        IOrderLineRepository orderLineRepository,
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderLineRepository = orderLineRepository;
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderResponse> Handle(UpdateOrderLineCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var line = await _orderLineRepository.GetByIdAsync(
            request.Request.Id,
            companyId,
            cancellationToken);

        if (line is null)
            throw new Exception("Order line tapılmadı.");

        var order = await _orderRepository.GetByIdAsync(line.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Sifariş tapılmadı.");

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new Exception("Bu sifarişin line-ı dəyişdirilə bilməz.");

        var oldLineTotal = line.LineTotal;
        var oldLineStatus = line.Status;

        line.Quantity = request.Request.Quantity;
        line.Note = request.Request.Note;

        if (!string.IsNullOrWhiteSpace(request.Request.Status))
        {
            if (!Enum.TryParse<OrderLineStatus>(request.Request.Status, true, out var parsedStatus))
                throw new Exception("Order line status düzgün deyil.");

            line.Status = parsedStatus;
        }

        line.LineTotal = line.UnitPrice * line.Quantity;

        if (oldLineStatus == OrderLineStatus.Cancelled && line.Status != OrderLineStatus.Cancelled)
        {
            order.TotalAmount += line.LineTotal;
        }
        else if (oldLineStatus != OrderLineStatus.Cancelled && line.Status == OrderLineStatus.Cancelled)
        {
            order.TotalAmount -= oldLineTotal;
        }
        else if (line.Status != OrderLineStatus.Cancelled)
        {
            order.TotalAmount = order.TotalAmount - oldLineTotal + line.LineTotal;
        }

        if (order.TotalAmount < 0)
            order.TotalAmount = 0;

        _orderLineRepository.Update(line);
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