using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.SetCounterparty;

public class SetOrderCounterpartyCommandHandler : IRequestHandler<SetOrderCounterpartyCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICounterpartyRepository _counterpartyRepository;
    private readonly ICurrentUserService _currentUserService;

    public SetOrderCounterpartyCommandHandler(
        IOrderRepository orderRepository,
        ICounterpartyRepository counterpartyRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _counterpartyRepository = counterpartyRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderResponse> Handle(SetOrderCounterpartyCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(request.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Sifariş tapılmadı.");

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new Exception("Bu sifariş dəyişdirilə bilməz.");

        if (request.CounterpartyId.HasValue)
        {
            var counterparty = await _counterpartyRepository.GetByIdAsync(
                request.CounterpartyId.Value, companyId, cancellationToken);

            if (counterparty is null)
                throw new Exception("Kontragent tapılmadı.");

            order.CounterpartyId = counterparty.Id;
        }
        else
        {
            order.CounterpartyId = null;
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
            CounterpartyId = updatedOrder.CounterpartyId,
            CounterpartyName = updatedOrder.Counterparty?.Name,
            OpenedAt = updatedOrder.OpenedAt,
            ClosedAt = updatedOrder.ClosedAt,
            TotalAmount = updatedOrder.TotalAmount,
            DiscountCode = updatedOrder.DiscountCode,
            DiscountAmount = updatedOrder.DiscountAmount,
            TableHourlyRate = updatedOrder.Table?.HourlyRate,
            TableRentalStartedAt = updatedOrder.TableRentalStartedAt,
            TableRentalStoppedAt = updatedOrder.TableRentalStoppedAt,
            TableRentalAmount = updatedOrder.TableRentalAmount,
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
                PreparationType = x.PreparationType,
                Note = x.Note,
                Status = x.Status.ToString(),
                ParentLineId = x.ParentLineId
            }).ToList()
        };
    }
}
