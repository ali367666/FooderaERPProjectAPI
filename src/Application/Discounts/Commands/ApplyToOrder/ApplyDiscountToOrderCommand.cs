using Application.Orders.Dtos;
using MediatR;

namespace Application.Discounts.Commands.ApplyToOrder;

public class ApplyDiscountToOrderCommand : IRequest<OrderResponse>
{
    public int OrderId { get; set; }
    public string Code { get; set; } = default!;
}
