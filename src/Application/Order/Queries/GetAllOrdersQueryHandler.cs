using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Queries.GetAll;

public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, List<OrderResponse>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllOrdersQueryHandler(
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<OrderResponse>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var orders = await _orderRepository.GetAllAsync(companyId, cancellationToken);
        var hasLegacyLineTotals = false;

        foreach (var order in orders)
        {
            foreach (var line in order.Lines)
            {
                var expectedLineTotal = line.UnitPrice * line.Quantity;
                if (line.LineTotal != expectedLineTotal)
                {
                    line.LineTotal = expectedLineTotal;
                    hasLegacyLineTotals = true;
                }
            }

            var expectedOrderTotal = order.Lines
                .Where(x => x.Status != OrderLineStatus.Cancelled)
                .Sum(x => x.LineTotal);

            if (order.TotalAmount != expectedOrderTotal)
            {
                order.TotalAmount = expectedOrderTotal;
                hasLegacyLineTotals = true;
            }
        }

        if (hasLegacyLineTotals)
            await _orderRepository.SaveChangesAsync(cancellationToken);

        return orders.Select(order => new OrderResponse
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
                Note = x.Note,
                Status = x.Status.ToString()
            }).ToList()
        }).ToList();
    }
}