using MediatR;

namespace Application.Orders.Commands.PrintKitchenTicket;

public record PrintKitchenTicketCommand(int OrderId, int PrinterId, string? Pin = null) : IRequest<int>;
