using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Discounts.Commands.RemoveFromOrder;

public class RemoveDiscountFromOrderCommandHandler : IRequestHandler<RemoveDiscountFromOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IDiscountRepository _discountRepository;
    private readonly ICurrentUserService _currentUser;

    public RemoveDiscountFromOrderCommandHandler(
        IOrderRepository orderRepository,
        IDiscountRepository discountRepository,
        ICurrentUserService currentUser)
    {
        _orderRepository = orderRepository;
        _discountRepository = discountRepository;
        _currentUser = currentUser;
    }

    public async Task<OrderResponse> Handle(RemoveDiscountFromOrderCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId;

        var order = await _orderRepository.GetByIdWithLinesAsync(request.OrderId, companyId, cancellationToken)
            ?? throw new Exception("Sifariş tapılmadı.");

        if (order.IsPaid)
            throw new Exception("Ödənilmiş sifarişdən endirim silinə bilməz.");

        if (order.DiscountId.HasValue)
        {
            var discount = await _discountRepository.GetByIdAsync(order.DiscountId.Value, companyId, cancellationToken);
            if (discount is not null && discount.UsedCount > 0)
            {
                discount.UsedCount--;
                _discountRepository.Update(discount);
            }
        }

        order.DiscountId = null;
        order.DiscountCode = null;
        order.DiscountAmount = 0;
        order.TotalAmount = order.Lines
            .Where(x => x.Status != OrderLineStatus.Cancelled)
            .Sum(x => x.LineTotal);

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
            Lines = order.Lines.Select(x => new OrderLineResponse
            {
                Id = x.Id,
                MenuItemId = x.MenuItemId,
                MenuItemName = x.MenuItem.Name,
                MenuItemType = x.MenuItem.PreparationType.ToString(),
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal,
                PreparationType = x.PreparationType,
                Status = x.Status.ToString(),
                Note = x.Note,
            }).ToList(),
        };
    }
}
