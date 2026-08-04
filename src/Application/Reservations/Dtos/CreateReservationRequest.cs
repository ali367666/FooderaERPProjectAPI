namespace Application.Reservations.Dtos;

public class CreateReservationRequest
{
    public int RestaurantId { get; set; }
    public int? TableId { get; set; }
    public string GuestName { get; set; } = default!;
    public string GuestPhone { get; set; } = default!;
    public string? GuestEmail { get; set; }
    public int GuestCount { get; set; }
    public DateTime ReservationDate { get; set; }
    public string ReservationTime { get; set; } = default!;  // "HH:mm"
    public int DurationMinutes { get; set; } = 90;
    public string? Note { get; set; }
}
