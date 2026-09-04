using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Discounts.Commands.ApplyToOrder;

public class ApplyDiscountToOrderCommandHandler : IRequestHandler<ApplyDiscountToOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IDiscountRepository _discountRepository;
    private readonly ICurrentUserService _currentUser;

    public ApplyDiscountToOrderCommandHandler(
        IOrderRepository orderRepository,
        IDiscountRepository discountRepository,
        ICurrentUserService currentUser)
    {
        _orderRepository = orderRepository;
        _discountRepository = discountRepository;
        _currentUser = currentUser;
    }

    public async Task<OrderResponse> Handle(ApplyDiscountToOrderCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId;

        var order = await _orderRepository.GetByIdWithLinesAsync(request.OrderId, companyId, cancellationToken)
            ?? throw new Exception("Sifariş tapılmadı.");

        if (order.IsPaid || order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new Exception("Ödənilmiş və ya ləğv edilmiş sifarişə endirim tətbiq etmək olmaz.");

        var code = request.Code.Trim().ToUpperInvariant();
        var discount = await _discountRepository.GetByCodeAsync(code, companyId, cancellationToken)
            ?? throw new Exception($"'{code}' kodlu endirim tapılmadı.");

        var now = DateTime.UtcNow;

        if (!discount.IsActive)
            throw new Exception("Bu endirim deaktivdir.");

        if (now.Date < discount.StartDate.Date || now.Date > discount.EndDate.Date)
            throw new Exception("Bu endirimin müddəti bitib və ya hələ başlamayıb.");

        if (discount.StartTime.HasValue && discount.EndTime.HasValue)
        {
            var nowTime = now.TimeOfDay;
            var withinTime = discount.StartTime.Value <= discount.EndTime.Value
                ? nowTime >= discount.StartTime.Value && nowTime <= discount.EndTime.Value
                : nowTime >= discount.StartTime.Value || nowTime <= discount.EndTime.Value;

            if (!withinTime)
                throw new Exception(
                    $"Bu endirim yalnız {discount.StartTime:hh\\:mm}-{discount.EndTime:hh\\:mm} arası keçərlidir.");
        }

        if (discount.MaxUsageCount.HasValue && discount.UsedCount >= discount.MaxUsageCount.Value)
            throw new Exception("Bu endirimin istifadə limiti bitib.");

        var subtotal = order.Lines
            .Where(x => x.Status != OrderLineStatus.Cancelled)
            .Sum(x => x.LineTotal);

        if (discount.MinOrderAmount.HasValue && subtotal < discount.MinOrderAmount.Value)
            throw new Exception($"Bu endirim minimum {discount.MinOrderAmount.Value:0.00} AZN sifariş üçün keçərlidir.");

        var discountAmount = discount.Type == DiscountType.Percentage
            ? subtotal * discount.Value / 100m
            : discount.Value;

        if (discount.MaxDiscountAmount.HasValue)
            discountAmount = Math.Min(discountAmount, discount.MaxDiscountAmount.Value);

        discountAmount = Math.Min(discountAmount, subtotal); // never exceed order total

        // If switching from a previously applied discount, release its usage slot
        if (order.DiscountId.HasValue && order.DiscountId != discount.Id)
        {
            var previous = await _discountRepository.GetByIdAsync(order.DiscountId.Value, companyId, cancellationToken);
            if (previous is not null && previous.UsedCount > 0)
            {
                previous.UsedCount--;
                _discountRepository.Update(previous);
            }
        }

        if (order.DiscountId != discount.Id)
        {
            discount.UsedCount++;
            _discountRepository.Update(discount);
        }

        order.DiscountId = discount.Id;
        order.DiscountCode = discount.Code;
        order.DiscountAmount = Math.Round(discountAmount, 2);
        order.TotalAmount = Math.Max(0, subtotal - order.DiscountAmount);

        _orderRepository.Update(order);
        await _discountRepository.SaveChangesAsync(cancellationToken);

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
            CounterpartyId = order.CounterpartyId,
            CounterpartyName = order.Counterparty?.Name,
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
            TableHourlyRate = order.Table?.HourlyRate,
            TableRentalStartedAt = order.TableRentalStartedAt,
            TableRentalStoppedAt = order.TableRentalStoppedAt,
            TableRentalAmount = order.TableRentalAmount,
            Lines = order.Lines.Select(x => new OrderLineResponse
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
                Status = x.Status.ToString(),
                Note = x.Note,
            }).ToList(),
        };
    }
}
