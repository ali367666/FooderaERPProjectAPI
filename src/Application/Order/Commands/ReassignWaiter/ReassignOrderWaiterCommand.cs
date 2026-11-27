using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.ReassignWaiter;

public record ReassignOrderWaiterCommand(int OrderId, int NewEmployeeId) : IRequest<OrderResponse>;
