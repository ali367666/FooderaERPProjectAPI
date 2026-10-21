namespace Application.Banquets.Dtos;

/// <summary>Books several tables of a hall for a banquet on a date and time.</summary>
public class CreateBanquetReservationRequest
{
    public int RestaurantId { get; set; }
    public List<int> TableIds { get; set; } = new();

    public string GuestName { get; set; } = default!;
    public string GuestPhone { get; set; } = default!;
    public int GuestCount { get; set; }

    public DateTime ReservationDate { get; set; }
    public string ReservationTime { get; set; } = default!;  // "HH:mm"
    public int DurationMinutes { get; set; } = 240;
    public string? Note { get; set; }
}
