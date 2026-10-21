using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reservations.Dtos;
using Domain.Constants;
using Domain.Enums;
using MediatR;

namespace Application.Reservations.Commands.Move;

public class MoveReservationCommandHandler : IRequestHandler<MoveReservationCommand, ReservationResponse>
{
    private readonly IReservationRepository _repo;
    private readonly IRestaurantTableRepository _tableRepo;
    private readonly ICurrentUserService _currentUser;

    public MoveReservationCommandHandler(
        IReservationRepository repo, IRestaurantTableRepository tableRepo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _tableRepo = tableRepo;
        _currentUser = currentUser;
    }

    public async Task<ReservationResponse> Handle(MoveReservationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(AppPermissions.ReservationUpdate))
            throw new BadRequestException("Bu əməliyyat üçün icazəniz yoxdur.");

        var r = await _repo.GetByIdAsync(request.Id, _currentUser.CompanyId, cancellationToken)
            ?? throw new BadRequestException("Rezervasiya tapılmadı.");

        if (r.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            throw new BadRequestException("Yalnız aktiv rezervasiyanı başqa masaya keçirmək olar.");
        if (r.IsBanquet)
            throw new BadRequestException("Banket rezervasiyasının masaları dəyişdirilə bilməz.");
        if (r.TableId == request.TableId)
            throw new BadRequestException("Rezervasiya artıq bu masadadır.");

        var table = await _tableRepo.GetByIdAsync(request.TableId, _currentUser.CompanyId, cancellationToken);
        if (table is null || !table.IsActive || table.RestaurantId != r.RestaurantId
            || table.Type is RestaurantTableType.Delivery or RestaurantTableType.TakeAway)
            throw new BadRequestException("Masa bu filial üçün tapılmadı.");

        var clash = await _repo.FindOverlappingAsync(
            _currentUser.CompanyId, table.Id, r.ReservationDate.Date + r.ReservationTime, r.DurationMinutes, r.Id, cancellationToken);
        if (clash is not null)
            throw new BadRequestException(
                $"{table.Name} masası {clash.ReservationTime:hh\\:mm} üçün {clash.GuestName} adına rezerv olunub.");

        r.TableId = table.Id;
        r.Table = table;
        _repo.Update(r);
        await _repo.SaveChangesAsync(cancellationToken);
        return ReservationMapper.ToResponse(r);
    }
}
