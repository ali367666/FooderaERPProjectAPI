using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Reservation : CompanyEntity<int>
{
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public int? TableId { get; set; }
    public RestaurantTable? Table { get; set; }

    public string GuestName { get; set; } = default!;
    public string GuestPhone { get; set; } = default!;
    public string? GuestEmail { get; set; }
    public int GuestCount { get; set; }

    public DateTime ReservationDate { get; set; }
    public TimeSpan ReservationTime { get; set; }
    public int DurationMinutes { get; set; } = 90;

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public string? Note { get; set; }

    public int? ConfirmedByUserId { get; set; }
    public User? ConfirmedByUser { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}
