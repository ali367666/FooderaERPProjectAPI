using MediatR;

namespace Application.Orders.Commands.Delete;

public record DeleteOrderCommand(int Id) : IRequest<string>;