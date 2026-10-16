using Domain.Enums;
using MediatR;

namespace Application.Discounts.Commands.SetManual;

/// <summary>
/// "₼" / "%" buttons on the order screen: the cashier types the discount themselves instead of
/// entering a discount code. Replaces any discount already on the order. Returns the discount in manat.
/// </summary>
public record SetManualDiscountCommand(int OrderId, DiscountType Type, decimal Value) : IRequest<decimal>;
