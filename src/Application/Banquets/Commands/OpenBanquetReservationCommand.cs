using Application.Orders.Dtos;
using MediatR;

namespace Application.Banquets.Commands;

/// <summary>The banquet day: turns a banquet booking into an open banquet order on its tables and seats the booking.</summary>
public record OpenBanquetReservationCommand(int ReservationId, int WaiterId) : IRequest<OrderResponse>;
