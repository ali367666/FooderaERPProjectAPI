using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Queries.GetReceipt;

public class GetOrderReceiptQueryHandler : IRequestHandler<GetOrderReceiptQuery, OrderReceiptResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetOrderReceiptQueryHandler(
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderReceiptResponse> Handle(GetOrderReceiptQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, _currentUserService.CompanyId, cancellationToken);
        if (order is null)
            throw new NotFoundException("Order not found.");

        foreach (var line in order.Lines)
            line.LineTotal = line.UnitPrice * line.Quantity;

        var totalAmount = order.Lines
            .Where(x => x.Status != OrderLineStatus.Cancelled)
            .Sum(x => x.LineTotal);

        return new OrderReceiptResponse
        {
            ReceiptNumber = order.ReceiptNumber ?? $"RCPT-{order.Id}",
            OrderNumber = order.OrderNumber,
            RestaurantName = order.Restaurant?.Name ?? "-",
            TableName = order.Table?.Name ?? "-",
            WaiterName = order.Waiter != null ? $"{order.Waiter.FirstName} {order.Waiter.LastName}" : "-",
            OpenedAt = order.OpenedAt,
            PaidAt = order.PaidAt,
            PaymentMethod = order.PaymentMethod?.ToString() ?? "-",
            TotalAmount = totalAmount,
            PaidAmount = order.PaidAmount,
            ChangeAmount = order.ChangeAmount,
            Lines = order.Lines
                .Where(x => x.Status != OrderLineStatus.Cancelled)
                .Select(x => new OrderReceiptLineResponse
                {
                    MenuItemName = x.MenuItem.Name,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    LineTotal = x.LineTotal
                })
                .ToList()
        };
    }
}
