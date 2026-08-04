using Application.Reservations.Dtos;
using MediatR;

namespace Application.Reservations.Commands.Create;

public class CreateReservationCommand : IRequest<ReservationResponse>
{
    public CreateReservationRequest Request { get; set; } = default!;
}
