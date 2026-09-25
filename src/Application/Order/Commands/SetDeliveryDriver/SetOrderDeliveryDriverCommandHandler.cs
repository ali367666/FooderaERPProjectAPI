using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.SetDeliveryDriver;

public class SetOrderDeliveryDriverCommandHandler : IRequestHandler<SetOrderDeliveryDriverCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public SetOrderDeliveryDriverCommandHandler(
        IOrderRepository orderRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderResponse> Handle(SetOrderDeliveryDriverCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(request.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Sifariş tapılmadı.");

        if (!order.IsDelivery)
            throw new Exception("Bu sifariş çatdırılma sifarişi deyil.");

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new Exception("Bu sifariş dəyişdirilə bilməz.");

        if (request.DriverEmployeeId is > 0)
        {
            var driver = await _employeeRepository.GetByIdAsync(request.DriverEmployeeId.Value, companyId, cancellationToken);
            if (driver is null)
                throw new Exception("Kuryer tapılmadı.");
            order.DeliveryDriverEmployeeId = driver.Id;
        }
        else
        {
            order.DeliveryDriverEmployeeId = null;
        }

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        var updatedOrder = await _orderRepository.GetByIdAsync(order.Id, companyId, cancellationToken);
        if (updatedOrder is null)
            throw new Exception("Yenilənmiş sifariş tapılmadı.");

        return new OrderResponse
        {
            Id = updatedOrder.Id,
            CompanyId = updatedOrder.CompanyId,
            OrderNumber = updatedOrder.OrderNumber,
            RestaurantId = updatedOrder.RestaurantId,
            RestaurantName = updatedOrder.Restaurant?.Name,
            TableId = updatedOrder.TableId,
            TableName = updatedOrder.Table?.Name,
            WaiterId = updatedOrder.WaiterId,
            WaiterName = updatedOrder.Waiter != null
                ? $"{updatedOrder.Waiter.FirstName} {updatedOrder.Waiter.LastName}"
                : null,
            Status = updatedOrder.Status.ToString(),
            Note = updatedOrder.Note,
            GuestCount = updatedOrder.GuestCount,
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
            IsDelivery = updatedOrder.IsDelivery,
            DeliveryAddress = updatedOrder.DeliveryAddress,
            DeliveryPhone = updatedOrder.DeliveryPhone,
            DeliveryDriverEmployeeId = updatedOrder.DeliveryDriverEmployeeId,
            DeliveryDriverName = updatedOrder.DeliveryDriverEmployee != null
                ? $"{updatedOrder.DeliveryDriverEmployee.FirstName} {updatedOrder.DeliveryDriverEmployee.LastName}"
                : null,
            Lines = updatedOrder.Lines.DistinctBy(x => x.Id).Select(x => new OrderLineResponse
            {
                Id = x.Id,
                MenuItemId = x.MenuItemId,
                MenuItemName = x.MenuItem.Name,
                MenuItemType = x.MenuItem.PreparationType.ToString(),
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
                PreparationType = x.PreparationType,
                Note = x.Note,
                Status = x.Status.ToString(),
                ParentLineId = x.ParentLineId
            }).ToList()
        };
    }
}
