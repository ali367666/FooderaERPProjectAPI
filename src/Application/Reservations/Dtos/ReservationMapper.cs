using Domain.Entities;

namespace Application.Reservations.Dtos;

public static class ReservationMapper
{
    public static ReservationResponse ToResponse(Reservation r) => new()
    {
        Id = r.Id,
        RestaurantId = r.RestaurantId,
        RestaurantName = r.Restaurant?.Name ?? string.Empty,
        TableId = r.TableId,
        TableName = r.Table?.Name,
        GuestName = r.GuestName,
        GuestPhone = r.GuestPhone,
        GuestEmail = r.GuestEmail,
        GuestCount = r.GuestCount,
        ReservationDate = r.ReservationDate,
        ReservationTime = r.ReservationTime.ToString(@"hh\:mm"),
        DurationMinutes = r.DurationMinutes,
        Status = r.Status.ToString(),
        Note = r.Note,
        ConfirmedAt = r.ConfirmedAt,
        CreatedAtUtc = r.CreatedAtUtc,
    };
}
