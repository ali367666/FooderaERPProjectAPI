using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.Update;

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateOrderCommandHandler(
        IOrderRepository orderRepository,
        IRestaurantTableRepository restaurantTableRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _restaurantTableRepository = restaurantTableRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderResponse> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(request.Request.Id, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Sifariş tapılmadı.");

        var table = await _restaurantTableRepository.GetByIdAsync(request.Request.TableId, companyId, cancellationToken);
        if (table is null)
            throw new Exception("Masa tapılmadı.");

        var waiter = await _employeeRepository.GetByIdAsync(request.Request.WaiterId, companyId, cancellationToken);
        if (waiter is null)
            throw new Exception("Garson tapılmadı.");

        if (!string.IsNullOrWhiteSpace(request.Request.Status))
        {
            if (!Enum.TryParse<OrderStatus>(request.Request.Status, true, out var parsedStatus))
                throw new Exception("Order status düzgün deyil.");

            order.Status = parsedStatus;

            if (parsedStatus == OrderStatus.Paid || parsedStatus == OrderStatus.Cancelled)
                order.ClosedAt = DateTime.UtcNow;
            else
                order.ClosedAt = null;
        }

        order.RestaurantId = request.Request.RestaurantId;
        order.TableId = request.Request.TableId;
        order.WaiterId = request.Request.WaiterId;
        order.Note = request.Request.Note;

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        var updatedOrder = await _orderRepository.GetByIdAsync(order.Id, companyId, cancellationToken);

        return new OrderResponse
        {
            Id = updatedOrder!.Id,
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
            TotalAmount = updatedOrder.Lines
                .Where(x => x.Status != OrderLineStatus.Cancelled)
                .Sum(x => x.UnitPrice * x.Quantity),
            Lines = updatedOrder.Lines.Select(x => new OrderLineResponse
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