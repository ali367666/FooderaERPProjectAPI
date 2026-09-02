using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Queries.GetById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetOrderByIdQueryHandler(
        IOrderRepository orderRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderResponse> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Sifariş tapılmadı.");

        if (!_currentUserService.HasPermission(Domain.Constants.AppPermissions.PosRedirectUser))
        {
            var currentEmployee = await _employeeRepository.GetByUserIdAsync(
                _currentUserService.UserId, companyId, cancellationToken);

            if (currentEmployee is null || order.WaiterId != currentEmployee.Id)
                throw new Exception("Bu sifariş başqa ofisiantə aiddir, baxa bilməzsiniz.");
        }

        var hasLegacyLineTotals = false;
        foreach (var line in order.Lines)
        {
            var expectedLineTotal = line.UnitPrice * line.Quantity;
            if (line.LineTotal != expectedLineTotal)
            {
                line.LineTotal = expectedLineTotal;
                hasLegacyLineTotals = true;
            }
        }

        var subtotal = order.Lines
            .DistinctBy(x => x.Id)
            .Where(x => x.Status != OrderLineStatus.Cancelled)
            .Sum(x => x.LineTotal);
        var expectedOrderTotal = order.IsPaid ? order.TotalAmount : Math.Max(0, subtotal - order.DiscountAmount);

        if (!order.IsPaid && order.TotalAmount != expectedOrderTotal)
        {
            order.TotalAmount = expectedOrderTotal;
            hasLegacyLineTotals = true;
        }

        if (hasLegacyLineTotals)
        {
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync(cancellationToken);
        }

        return new OrderResponse
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
            GuestCount = order.GuestCount,
            OpenedAt = order.OpenedAt,
            ClosedAt = order.ClosedAt,
            TotalAmount = order.TotalAmount,
            DiscountCode = order.DiscountCode,
            DiscountAmount = order.DiscountAmount,
            IsPaid = order.IsPaid,
            PaidAt = order.PaidAt,
            PaymentMethod = order.PaymentMethod?.ToString(),
            PaidAmount = order.PaidAmount,
            ChangeAmount = order.ChangeAmount,
            ReceiptNumber = order.ReceiptNumber,
            Lines = order.Lines.DistinctBy(x => x.Id).Select(x => new OrderLineResponse
            {
                Id = x.Id,
                MenuItemId = x.MenuItemId,
                MenuItemName = x.MenuItem.Name,
                MenuItemType = x.MenuItem.PreparationType.ToString(),
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal,
                HoldUntilUtc = x.HoldUntilUtc,
                PreparationType = x.PreparationType,
                Note = x.Note,
                Status = x.Status.ToString(),
                ParentLineId = x.ParentLineId
            }).ToList()
        };
    }
}