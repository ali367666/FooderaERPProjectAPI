using MediatR;

namespace Application.Orders.Commands.PrintKitchenTicket;

public record PrintKitchenTicketCommand(int OrderId, int PrinterId) : IRequest<int>;
