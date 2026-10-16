using Application.Reservations.Dtos;
using MediatR;

namespace Application.Reservations.Commands.Move;

public class MoveReservationCommand : IRequest<ReservationResponse>
{
    public int Id { get; set; }
    public int TableId { get; set; }
}
