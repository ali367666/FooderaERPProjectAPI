using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Queries.GetById;

public record GetOrderByIdQuery(int Id) : IRequest<OrderResponse>;