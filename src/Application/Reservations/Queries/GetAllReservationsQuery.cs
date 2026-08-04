using Application.Reservations.Dtos;
using MediatR;

namespace Application.Reservations.Queries;

public class GetAllReservationsQuery : IRequest<List<ReservationResponse>>
{
    public DateTime? Date { get; set; }
}
