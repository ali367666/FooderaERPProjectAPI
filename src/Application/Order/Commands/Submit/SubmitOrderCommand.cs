using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.Submit;

public record SubmitOrderCommand(int OrderId) : IRequest<OrderResponse>;
