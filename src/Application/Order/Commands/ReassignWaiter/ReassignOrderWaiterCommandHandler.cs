using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Domain.Constants;
using Application.Common.Models;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.ReassignWaiter;

public class ReassignOrderWaiterCommandHandler : IRequestHandler<ReassignOrderWaiterCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IStaffCodeResolver _staffCodeResolver;
    private readonly IUserRepository _userRepository;

    public ReassignOrderWaiterCommandHandler(
        IOrderRepository orderRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IStaffCodeResolver staffCodeResolver,
        IUserRepository userRepository)
    {
        _orderRepository = orderRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _staffCodeResolver = staffCodeResolver;
        _userRepository = userRepository;
    }

    public async Task<OrderResponse> Handle(ReassignOrderWaiterCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        // Anyone may press "Ofisiant dəyiş", but only someone with Pos.RedirectUser may approve it:
        // either the caller themselves or a supervisor who types their code.
        string? approvedBy = null;
        if (!_currentUserService.HasPermission(AppPermissions.PosRedirectUser))
        {
            if (string.IsNullOrWhiteSpace(request.SupervisorCode))
                throw new BadRequestException("Ofisiantı dəyişmək üçün səlahiyyətli şəxsin kodunu daxil edin.");

            var approver = await _staffCodeResolver.ResolveAsync(companyId, request.SupervisorCode.Trim(), cancellationToken);
            if (approver is null || !await _userRepository.HasPermissionAsync(approver.Id, AppPermissions.PosRedirectUser, cancellationToken))
                throw new BadRequestException("Kod yanlışdır və ya bu əməliyyat üçün icazəniz yoxdur.");

            approvedBy = approver.UserName;
        }

        var order = await _orderRepository.GetByIdAsync(request.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Order not found.");

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new Exception("This order can no longer be reassigned.");

        var newEmployee = await _employeeRepository.GetByIdAsync(request.NewEmployeeId, companyId, cancellationToken);
        if (newEmployee is null)
            throw new Exception("Employee not found.");

        var oldWaiterId = order.WaiterId;
        order.WaiterId = newEmployee.Id;

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Order",
                    EntityId = order.Id.ToString(),
                    ActionType = "ReassignWaiter",
                    Message = $"Order {order.Id} ofisiantı dəyişdirildi: {oldWaiterId} -> {newEmployee.Id}"
                        + (approvedBy is null ? "" : $" (təsdiq edən: {approvedBy})"),
                    IsSuccess = true
                },
                cancellationToken);
        }
        catch
        {
            // audit log failures must not block the operation
        }

        var updatedOrder = await _orderRepository.GetByIdAsync(order.Id, companyId, cancellationToken);
        if (updatedOrder is null)
            throw new Exception("Updated order not found.");

        return new OrderResponse
        {
            Id = updatedOrder.Id,
            OrderNumber = updatedOrder.OrderNumber,
            RestaurantId = updatedOrder.RestaurantId,
            TableId = updatedOrder.TableId,
            TableName = updatedOrder.Table?.Name,
            WaiterId = updatedOrder.WaiterId,
            WaiterName = updatedOrder.Waiter != null ? $"{updatedOrder.Waiter.FirstName} {updatedOrder.Waiter.LastName}" : null,
            Status = updatedOrder.Status.ToString(),
            Note = updatedOrder.Note,
            GuestCount = updatedOrder.GuestCount,
            CounterpartyId = updatedOrder.CounterpartyId,
            CounterpartyName = updatedOrder.Counterparty?.Name,
            CounterpartyDebtAmount = updatedOrder.Counterparty?.CurrentDebtAmount,
            OpenedAt = updatedOrder.OpenedAt,
            ClosedAt = updatedOrder.ClosedAt,
            TotalAmount = updatedOrder.TotalAmount,
            DiscountCode = updatedOrder.DiscountCode,
            DiscountAmount = updatedOrder.DiscountAmount,
            TableHourlyRate = updatedOrder.Table?.HourlyRate,
            TableRentalStartedAt = updatedOrder.TableRentalStartedAt,
            TableRentalStoppedAt = updatedOrder.TableRentalStoppedAt,
            TableRentalAmount = updatedOrder.TableRentalAmount,
            HoldUntilUtc = updatedOrder.HoldUntilUtc,
            BillPrintedAt = updatedOrder.BillPrintedAt,
            IsBillLocked = updatedOrder.IsBillLocked,
            IsDelivery = updatedOrder.IsDelivery,
            DeliveryAddress = updatedOrder.DeliveryAddress,
            DeliveryPhone = updatedOrder.DeliveryPhone,
            DeliveryDriverEmployeeId = updatedOrder.DeliveryDriverEmployeeId,
            DeliveryDriverName = updatedOrder.DeliveryDriverEmployee != null ? $"{updatedOrder.DeliveryDriverEmployee.FirstName} {updatedOrder.DeliveryDriverEmployee.LastName}" : null,
            Lines = updatedOrder.Lines.Select(x => new OrderLineResponse
            {
                Id = x.Id,
                MenuItemId = x.MenuItemId,
                MenuItemName = x.MenuItem.Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal,
                HoldUntilUtc = x.HoldUntilUtc,
                KitchenPrintedAt = x.KitchenPrintedAt,
                TimeBasedStartedAt = x.TimeBasedStartedAt,
                TimeBasedStoppedAt = x.TimeBasedStoppedAt,
                IsTimeBased = x.MenuItem.IsTimeBased,
                IsWeightBased = Application.Common.Helpers.OrderLinePricing.IsWeightBased(x.MenuItem.UnitId),
                IsGift = x.IsGift,
                DiscountAmount = x.DiscountAmount,
                Note = x.Note,
                Status = x.Status.ToString()
            }).ToList()
        };
    }
}
