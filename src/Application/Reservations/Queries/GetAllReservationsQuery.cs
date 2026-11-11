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

    /// <summary>
    /// Narrow the days to a time range: from FromTime on the first day to the end of the ToTime minute on the last day (HH:mm). A date that
    /// itself carries a time of day counts as that time. With no day at all, the time of day applies to every day.
    /// </summary>
    public TimeSpan? FromTime { get; set; }
    public TimeSpan? ToTime { get; set; }
}
