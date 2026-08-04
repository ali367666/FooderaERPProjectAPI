using MediatR;

namespace Application.Reservations.Commands.Delete;

public class DeleteReservationCommand : IRequest
{
    public int Id { get; set; }
}
