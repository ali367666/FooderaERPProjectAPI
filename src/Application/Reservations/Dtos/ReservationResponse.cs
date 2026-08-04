namespace Application.Reservations.Dtos;

public class ReservationResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = default!;
    public int? TableId { get; set; }
    public string? TableName { get; set; }
    public string GuestName { get; set; } = default!;
    public string GuestPhone { get; set; } = default!;
    public string? GuestEmail { get; set; }
    public int GuestCount { get; set; }
    public DateTime ReservationDate { get; set; }
    public string ReservationTime { get; set; } = default!;  // "HH:mm"
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = default!;
    public string? Note { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
