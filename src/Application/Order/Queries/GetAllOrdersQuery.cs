using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Queries.GetAll;

public record GetAllOrdersQuery : IRequest<List<OrderResponse>>;