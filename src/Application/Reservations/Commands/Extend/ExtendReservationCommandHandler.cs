using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reservations.Dtos;
using Domain.Constants;
using Domain.Enums;
using MediatR;

namespace Application.Reservations.Commands.Extend;

public class ExtendReservationCommandHandler : IRequestHandler<ExtendReservationCommand, ReservationResponse>
{
    private const int MaxDurationMinutes = 24 * 60;

    private readonly IReservationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public ExtendReservationCommandHandler(IReservationRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<ReservationResponse> Handle(ExtendReservationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(AppPermissions.ReservationUpdate))
            throw new BadRequestException("Bu əməliyyat üçün icazəniz yoxdur.");
        if (request.Minutes <= 0)
            throw new BadRequestException("Artırılacaq müddət müsbət olmalıdır.");

        var r = await _repo.GetByIdAsync(request.Id, _currentUser.CompanyId, cancellationToken)
            ?? throw new BadRequestException("Rezervasiya tapılmadı.");

        if (r.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            throw new BadRequestException("Yalnız aktiv rezervasiyanın müddətini artırmaq olar.");

        var newDuration = Math.Min(r.DurationMinutes + request.Minutes, MaxDurationMinutes);

        if (r.TableId is int tableId)
        {
            var clash = await _repo.FindOverlappingAsync(
                _currentUser.CompanyId, tableId, r.ReservationDate.Date + r.ReservationTime, newDuration, r.Id, cancellationToken);
            if (clash is not null)
                throw new BadRequestException(
                    $"Müddəti artırmaq olmaz: masa {clash.ReservationTime:hh\\:mm} üçün {clash.GuestName} adına rezerv olunub.");
        }

        r.DurationMinutes = newDuration;
        _repo.Update(r);
        await _repo.SaveChangesAsync(cancellationToken);
        return ReservationMapper.ToResponse(r);
    }
}
