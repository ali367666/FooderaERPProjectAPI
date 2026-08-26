using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.MoveTable;

public record MoveOrderTableCommand(int OrderId, int NewTableId) : IRequest<OrderResponse>;
