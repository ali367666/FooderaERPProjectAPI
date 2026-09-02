using MediatR;

namespace Application.Orders.Commands.DiscardEmpty;

public record DiscardEmptyOrderCommand(int OrderId) : IRequest;
