using MediatR;

namespace Application.Orders.Commands.Delete;

public record DeleteOrderCommand(int Id, string? Reason = null, string? Note = null) : IRequest<string>;