using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Orders.Commands.Create;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IRestaurantTableRepository restaurantTableRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _restaurantTableRepository = restaurantTableRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "CreateOrderCommand başladı. RestaurantId: {RestaurantId}, TableId: {TableId}, WaiterId: {WaiterId}, CompanyId: {CompanyId}",
            request.Request.RestaurantId,
            request.Request.TableId,
            request.Request.WaiterId,
            companyId);

        var table = await _restaurantTableRepository.GetByIdAsync(
            request.Request.TableId,
            companyId,
            cancellationToken);

        if (table is null)
        {
            _logger.LogWarning(
                "Order yaradılmadı. Masa tapılmadı. TableId: {TableId}, CompanyId: {CompanyId}",
                request.Request.TableId,
                companyId);

            throw new Exception("Masa tapılmadı.");
        }

        var waiter = await _employeeRepository.GetByIdAsync(
            request.Request.WaiterId,
            companyId,
            cancellationToken);

        if (waiter is null)
        {
            _logger.LogWarning(
                "Order yaradılmadı. Garson tapılmadı. WaiterId: {WaiterId}, CompanyId: {CompanyId}",
                request.Request.WaiterId,
                companyId);

            throw new Exception("Garson tapılmadı.");
        }

        var hasOpenOrder = await _orderRepository.HasOpenOrderForTableAsync(
            request.Request.TableId,
            companyId,
            cancellationToken);

        if (hasOpenOrder)
        {
            _logger.LogWarning(
                "Order yaradılmadı. Masa üçün artıq açıq sifariş var. TableId: {TableId}, CompanyId: {CompanyId}",
                request.Request.TableId,
                companyId);

            throw new Exception("Bu masa üçün artıq açıq sifariş mövcuddur.");
        }

        var orderNumber = await GenerateOrderNumberAsync(companyId, cancellationToken);

        var order = new Domain.Entities.Order
        {
            CompanyId = companyId,
            OrderNumber = orderNumber,
            RestaurantId = request.Request.RestaurantId,
            TableId = request.Request.TableId,
            WaiterId = request.Request.WaiterId,
            Status = OrderStatus.Draft,
            Note = string.IsNullOrWhiteSpace(request.Request.Note)
                ? null
                : request.Request.Note.Trim(),
            OpenedAt = DateTime.UtcNow,
            TotalAmount = 0
        };

        await _orderRepository.AddAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Order",
                    EntityId = order.Id.ToString(),
                    ActionType = "Create",
                    Message = $"Order yaradıldı. Id: {order.Id}, OrderNumber: {order.OrderNumber}, RestaurantId: {order.RestaurantId}, TableId: {order.TableId}, WaiterId: {order.WaiterId}, Status: {order.Status}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "Order üçün audit log yazıldı. OrderId: {OrderId}",
                order.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "Order create audit log yazılarkən xəta baş verdi. OrderId: {OrderId}",
                order.Id);
        }

        _logger.LogInformation(
            "Order uğurla yaradıldı. OrderId: {OrderId}, OrderNumber: {OrderNumber}, CompanyId: {CompanyId}",
            order.Id,
            order.OrderNumber,
            companyId);

        return new OrderResponse
        {
            Id = order.Id,
            CompanyId = order.CompanyId,
            OrderNumber = order.OrderNumber,
            RestaurantId = order.RestaurantId,
            RestaurantName = table.Restaurant?.Name,
            TableId = order.TableId,
            TableName = table.Name,
            WaiterId = order.WaiterId,
            WaiterName = $"{waiter.FirstName} {waiter.LastName}",
            ProcessedByUserId = order.ProcessedByUserId,
            ProcessedByUserName = null,
            ProcessedAt = order.ProcessedAt,
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