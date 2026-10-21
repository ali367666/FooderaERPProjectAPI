using Domain.Common;

namespace Domain.Entities;

/// <summary>A table held by a banquet reservation (every one of them, the reservation's main table included).</summary>
public class ReservationTable : CompanyEntity<int>
{
    public int ReservationId { get; set; }
    public Reservation Reservation { get; set; } = default!;

    public int TableId { get; set; }
    public RestaurantTable Table { get; set; } = default!;
}
