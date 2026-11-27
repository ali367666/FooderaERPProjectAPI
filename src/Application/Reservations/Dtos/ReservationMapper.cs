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
        IsBanquet = r.IsBanquet,
        BanquetHallId = r.IsBanquet ? r.BanquetTables.Select(b => b.Table?.SectionId).FirstOrDefault(x => x != null) : null,
        BanquetHallName = r.IsBanquet ? r.BanquetTables.Select(b => b.Table?.Section?.Name).FirstOrDefault(x => x != null) : null,
        BanquetTables = r.BanquetTables
            .Select(b => new Application.Banquets.Dtos.BanquetTableRef
            {
                Id = b.TableId,
                Name = b.Table?.Name ?? string.Empty,
                Capacity = b.Table?.Capacity ?? 0,
                SectionId = b.Table?.SectionId
            })
            .ToList(),
        ConfirmedAt = r.ConfirmedAt,
        CreatedAtUtc = r.CreatedAtUtc,
    };
}
