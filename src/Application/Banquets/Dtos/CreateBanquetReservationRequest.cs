namespace Application.Banquets.Dtos;

/// <summary>
/// Books a banquet hall for a date and time. Pick the hall with <see cref="SectionId"/> and every table of it
/// is held; <see cref="TableIds"/> is only the older way of booking an explicit set of tables.
/// </summary>
public class CreateBanquetReservationRequest
{
    public int RestaurantId { get; set; }
    public int? SectionId { get; set; }
    public List<int> TableIds { get; set; } = new();

    public string GuestName { get; set; } = default!;
    public string GuestPhone { get; set; } = default!;
    public int GuestCount { get; set; }

    public DateTime ReservationDate { get; set; }
    public string ReservationTime { get; set; } = default!;  // "HH:mm"
    public int DurationMinutes { get; set; } = 240;
    public string? Note { get; set; }
}
