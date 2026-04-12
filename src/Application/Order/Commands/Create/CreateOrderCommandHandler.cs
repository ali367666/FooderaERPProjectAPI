using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.Create;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateOrderCommandHandler(
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

    public async Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var table = await _restaurantTableRepository.GetByIdAsync(
            request.Request.TableId,
            companyId,
            cancellationToken);

        if (table is null)
            throw new Exception("Masa tapılmadı.");

        var waiter = await _employeeRepository.GetByIdAsync(
            request.Request.WaiterId,
            companyId,
            cancellationToken);

        if (waiter is null)
            throw new Exception("Garson tapılmadı.");

        var hasOpenOrder = await _orderRepository.HasOpenOrderForTableAsync(
            request.Request.TableId,
            companyId,
            cancellationToken);

        if (hasOpenOrder)
            throw new Exception("Bu masa üçün artıq açıq sifariş mövcuddur.");

        var orderNumber = await GenerateOrderNumberAsync(companyId, cancellationToken);

        var order = new Domain.Entities.Order
        {
            CompanyId = companyId,
            OrderNumber = orderNumber,
            RestaurantId = request.Request.RestaurantId,
            TableId = request.Request.TableId,
            WaiterId = request.Request.WaiterId,
            Status = OrderStatus.Open,
            Note = request.Request.Note,
            OpenedAt = DateTime.UtcNow,
            TotalAmount = 0
        };

        await _orderRepository.AddAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            RestaurantId = order.RestaurantId,
            TableId = order.TableId,
            TableName = table.Name,
            WaiterId = order.WaiterId,
            WaiterName = $"{waiter.FirstName} {waiter.LastName}",
            Status = order.Status.ToString(),
            Note = order.Note,
            OpenedAt = order.OpenedAt,
            ClosedAt = order.ClosedAt,
            TotalAmount = 0,
            Lines = new List<OrderLineResponse>()
        };
    }

    private async Task<string> GenerateOrderNumberAsync(int companyId, CancellationToken cancellationToken)
    {
        string orderNumber;

        do
        {
            orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        }
        while (await _orderRepository.ExistsByOrderNumberAsync(orderNumber, companyId, cancellationToken));

        return orderNumber;
    }
}