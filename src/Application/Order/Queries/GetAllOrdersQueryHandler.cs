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

        return orders.Select(order => new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            RestaurantId = order.RestaurantId,
            TableId = order.TableId,
            TableName = order.Table?.Name,
            WaiterId = order.WaiterId,
            WaiterName = order.Waiter != null ? $"{order.Waiter.FirstName} {order.Waiter.LastName}" : null,
            Status = order.Status.ToString(),
            Note = order.Note,
            OpenedAt = order.OpenedAt,
            ClosedAt = order.ClosedAt,
            TotalAmount = order.Lines
                .Where(x => x.Status != OrderLineStatus.Cancelled)
                .Sum(x => x.UnitPrice * x.Quantity),
            Lines = order.Lines.Select(x => new OrderLineResponse
            {
                Id = x.Id,
                MenuItemId = x.MenuItemId,
                MenuItemName = x.MenuItem.Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                Note = x.Note,
                Status = x.Status.ToString()
            }).ToList()
        }).ToList();
    }
}