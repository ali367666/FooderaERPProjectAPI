using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Queries.GetReceipt;

public record GetOrderReceiptQuery(int OrderId) : IRequest<OrderReceiptResponse>;
