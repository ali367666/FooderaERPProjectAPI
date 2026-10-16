using Application.Reservations.Dtos;
using MediatR;

namespace Application.Reservations.Commands.Extend;

public class ExtendReservationCommand : IRequest<ReservationResponse>
{
    public int Id { get; set; }
    public int Minutes { get; set; }
}
