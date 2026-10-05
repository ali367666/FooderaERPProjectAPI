using Application.Common.Helpers;
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
    private readonly IOrderCancellationRepository _cancellationRepository;
    private readonly ILogger<DeleteOrderCommandHandler> _logger;

    public DeleteOrderCommandHandler(
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IOrderCancellationRepository cancellationRepository,
        ILogger<DeleteOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _cancellationRepository = cancellationRepository;
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

        OrderGuards.EnsureNotBillLocked(order);

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

        // A deleted receipt disappears entirely — keep it in the cancellations report (unless it was
        // already cancelled, which logged it then).
        if (order.Status != Domain.Enums.OrderStatus.Cancelled)
            await _cancellationRepository.AddAsync(
                OrderCancellations.ForOrder(order, OrderCancellations.Clean(request.Reason) ?? "Çek silindi",
                    request.Note, _currentUserService.UserId),
                cancellationToken);

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