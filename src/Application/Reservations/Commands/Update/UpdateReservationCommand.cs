using Application.Reservations.Dtos;
using MediatR;

namespace Application.Reservations.Commands.Update;

public class UpdateReservationCommand : IRequest<ReservationResponse>
{
    public int Id { get; set; }
    public UpdateReservationRequest Request { get; set; } = default!;
}
