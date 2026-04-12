using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Queries.GetById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetOrderByIdQueryHandler(
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderResponse> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Sifariş tapılmadı.");

        return new OrderResponse
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
        };
    }
}