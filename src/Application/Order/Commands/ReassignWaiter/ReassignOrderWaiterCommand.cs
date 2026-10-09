using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.ReassignWaiter;

/// <param name="SupervisorCode">POS code of someone allowed to redirect orders — required when the caller lacks Pos.RedirectUser itself.</param>
public record ReassignOrderWaiterCommand(int OrderId, int NewEmployeeId, string? SupervisorCode = null) : IRequest<OrderResponse>;
