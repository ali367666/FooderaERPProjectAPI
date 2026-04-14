using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Orders.Commands.Delete;

public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, string>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteOrderCommandHandler> _logger;

    public DeleteOrderCommandHandler(
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ILogger<DeleteOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<string> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        _logger.LogInformation(
            "DeleteOrderCommand başladı. OrderId: {OrderId}, CompanyId: {CompanyId}",
            request.Id,
            companyId);

        var order = await _orderRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (order is null)
        {
            _logger.LogWarning(
                "Order silinmədi. Tapılmadı. OrderId: {OrderId}, CompanyId: {CompanyId}",
                request.Id,
                companyId);

            throw new Exception("Sifariş tapılmadı.");
        }

        var oldValues = JsonSerializer.Serialize(new
        {
            order.Id,
            order.OrderNumber,
            order.RestaurantId,
            order.TableId,
            order.WaiterId,
            order.Status,
            order.TotalAmount,
            order.OpenedAt,
            order.ClosedAt
        });

        _orderRepository.Delete(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Order",
                    EntityId = order.Id.ToString(),
                    ActionType = "Delete",
                    OldValues = oldValues,
                    NewValues = null,
                    Message = $"Order silindi. Id: {order.Id}, OrderNumber: {order.OrderNumber}, Status: {order.Status}, TotalAmount: {order.TotalAmount}",
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
                "Order delete audit log yazılarkən xəta baş verdi. OrderId: {OrderId}",
                order.Id);
        }

        _logger.LogInformation(
            "Order uğurla silindi. OrderId: {OrderId}, CompanyId: {CompanyId}",
            order.Id,
            companyId);

        return "Sifariş uğurla silindi.";
    }
}