using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;

namespace Application.Reservations.Services;

public sealed class ReservationTableGuard : IReservationTableGuard
{
    private readonly ICompanySettingsRepository _settingsRepository;
    private readonly IReservationRepository _reservationRepository;

    public ReservationTableGuard(
        ICompanySettingsRepository settingsRepository,
        IReservationRepository reservationRepository)
    {
        _settingsRepository = settingsRepository;
        _reservationRepository = reservationRepository;
    }

    public async Task EnsureTableFreeAsync(int companyId, int tableId, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        var blockMinutes = settings?.ReservationBlockMinutes ?? 0;
        if (blockMinutes <= 0)
            return;

        var reservation = await _reservationRepository.FindBlockingForTableAsync(
            companyId, tableId, BusinessTime.Now, blockMinutes, cancellationToken);
        if (reservation is null)
            return;

        throw new BadRequestException(
            $"Bu masa {reservation.ReservationTime:hh\\:mm} üçün {reservation.GuestName} adına rezerv olunub — sifariş açmaq olmaz.");
    }
}
