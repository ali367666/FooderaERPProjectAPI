using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reservations.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Reservations.Commands.ChangeStatus;

public class ChangeReservationStatusCommandHandler
    : IRequestHandler<ChangeReservationStatusCommand, ReservationResponse>
{
    private readonly IReservationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public ChangeReservationStatusCommandHandler(IReservationRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<ReservationResponse> Handle(ChangeReservationStatusCommand request, CancellationToken cancellationToken)
    {
        var action = request.Action.ToLower();

        var requiredPermission = action == "cancel"
            ? Domain.Constants.AppPermissions.ReservationCancel
            : Domain.Constants.AppPermissions.ReservationConfirm;
        if (!_currentUser.HasPermission(requiredPermission))
            throw new Exception("Bu əməliyyat üçün icazəniz yoxdur.");

        var r = await _repo.GetByIdAsync(request.Id, _currentUser.CompanyId, cancellationToken)
            ?? throw new Exception("Rezervasiya tapılmadı.");

        r.Status = action switch
        {
            "confirm" => ReservationStatus.Confirmed,
            "seat"    => ReservationStatus.Seated,
            "complete"=> ReservationStatus.Completed,
            "cancel"  => ReservationStatus.Cancelled,
            "noshow"  => ReservationStatus.NoShow,
            _ => throw new Exception($"Naməlum əməliyyat: {request.Action}"),
        };

        if (action == "confirm")
        {
            r.ConfirmedByUserId = _currentUser.UserId;
            r.ConfirmedAt = DateTime.UtcNow;
        }

        _repo.Update(r);
        await _repo.SaveChangesAsync(cancellationToken);
        return ReservationMapper.ToResponse(r);
    }
}
