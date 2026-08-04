namespace Application.Reservations.Dtos;

public class UpdateReservationRequest
{
    public int? TableId { get; set; }
    public string GuestName { get; set; } = default!;
    public string GuestPhone { get; set; } = default!;
    public string? GuestEmail { get; set; }
    public int GuestCount { get; set; }
    public DateTime ReservationDate { get; set; }
    public string ReservationTime { get; set; } = default!;
    public int DurationMinutes { get; set; } = 90;
    public string? Note { get; set; }
}
