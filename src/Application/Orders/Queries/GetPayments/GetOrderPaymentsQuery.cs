using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Queries.GetPayments;

public record GetOrderPaymentsQuery(int OrderId) : IRequest<OrderPaymentsResponse>;
