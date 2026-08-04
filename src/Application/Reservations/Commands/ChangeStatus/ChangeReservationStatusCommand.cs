using Application.Reservations.Dtos;
using MediatR;

namespace Application.Reservations.Commands.ChangeStatus;

public class ChangeReservationStatusCommand : IRequest<ReservationResponse>
{
    public int Id { get; set; }
    public string Action { get; set; } = default!; // confirm | seat | complete | cancel | noshow
}
