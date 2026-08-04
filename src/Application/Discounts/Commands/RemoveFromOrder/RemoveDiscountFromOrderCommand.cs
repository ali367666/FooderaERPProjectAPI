using Application.Orders.Dtos;
using MediatR;

namespace Application.Discounts.Commands.RemoveFromOrder;

public class RemoveDiscountFromOrderCommand : IRequest<OrderResponse>
{
    public int OrderId { get; set; }
}
