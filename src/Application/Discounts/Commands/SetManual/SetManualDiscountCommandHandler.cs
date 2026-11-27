using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.Discounts.Commands.SetManual;

public class SetManualDiscountCommandHandler : IRequestHandler<SetManualDiscountCommand, decimal>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IDiscountRepository _discountRepository;
    private readonly ICurrentUserService _currentUser;

    public SetManualDiscountCommandHandler(
        IOrderRepository orderRepository,
        IDiscountRepository discountRepository,
        ICurrentUserService currentUser)
    {
        _orderRepository = orderRepository;
        _discountRepository = discountRepository;
        _currentUser = currentUser;
    }

    public async Task<decimal> Handle(SetManualDiscountCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId;

        var order = await _orderRepository.GetByIdWithLinesAsync(request.OrderId, companyId, cancellationToken)
            ?? throw new NotFoundException("Sifariş tapılmadı.");

        OrderGuards.EnsureNotBillLocked(order);

        if (order.IsPaid || order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new BadRequestException("Ödənilmiş və ya ləğv edilmiş sifarişə endirim tətbiq etmək olmaz.");

        var subtotal = order.Lines
            .DistinctBy(x => x.Id)
            .Where(x => x.Status != OrderLineStatus.Cancelled)
            .Sum(x => x.LineTotal);

        if (subtotal <= 0)
            throw new BadRequestException("Endirim üçün sifarişdə məhsul olmalıdır.");

        // Products (or whole categories) marked "not in discounts" are left out of what the discount is worked on.
        var excluded = await _discountRepository.GetDiscountExcludedMenuItemIdsAsync(
            companyId, order.Lines.Select(x => x.MenuItemId).Distinct().ToList(), cancellationToken);
        var discountable = order.Lines
            .DistinctBy(x => x.Id)
            .Where(x => x.Status != OrderLineStatus.Cancelled && !excluded.Contains(x.MenuItemId))
            .Sum(x => x.LineTotal);
        if (discountable <= 0)
            throw new BadRequestException("Sifarişdəki məhsulların heç biri endirimdə iştirak etmir.");

        var amount = request.Type == DiscountType.Percentage
            ? discountable * request.Value / 100m
            : request.Value;

        if (request.Type == DiscountType.FixedAmount && amount > discountable)
            throw new BadRequestException(discountable < subtotal
                ? $"Endirim endirimdə iştirak edən məhsulların cəmindən ({discountable:0.00} ₼) çox ola bilməz."
                : $"Endirim sifarişin cəmindən ({subtotal:0.00} ₼) çox ola bilməz.");

        amount = Math.Round(Math.Min(amount, discountable), 2, MidpointRounding.AwayFromZero);

        // A discount code may be on the order already — release its usage slot, the manual one replaces it.
        if (order.DiscountId.HasValue)
        {
            var previous = await _discountRepository.GetByIdAsync(order.DiscountId.Value, companyId, cancellationToken);
            if (previous is not null && previous.UsedCount > 0)
            {
                previous.UsedCount--;
                _discountRepository.Update(previous);
            }
        }

        order.DiscountId = null;
        order.DiscountCode = request.Type == DiscountType.Percentage
            ? $"{request.Value:0.##}%"
            : $"{request.Value:0.00} ₼";
        order.DiscountAmount = amount;
        order.TotalAmount = Math.Max(0, subtotal - amount);

        _orderRepository.Update(order);
        await _discountRepository.SaveChangesAsync(cancellationToken);

        return amount;
    }
}
