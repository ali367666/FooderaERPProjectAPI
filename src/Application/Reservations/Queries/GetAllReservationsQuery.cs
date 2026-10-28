using Application.Reservations.Dtos;
using MediatR;

namespace Application.Reservations.Queries;

public class GetAllReservationsQuery : IRequest<List<ReservationResponse>>
{
    /// <summary>A single day; if sent together with the range below, all conditions apply.</summary>
    public DateTime? Date { get; set; }
    /// <summary>First day of the range, inclusive.</summary>
    public DateTime? DateFrom { get; set; }
    /// <summary>Last day of the range, inclusive.</summary>
    public DateTime? DateTo { get; set; }
}
