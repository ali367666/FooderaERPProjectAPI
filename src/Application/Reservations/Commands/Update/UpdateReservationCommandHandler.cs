using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reservations.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Reservations.Commands.Update;

public class UpdateReservationCommandHandler
    : IRequestHandler<UpdateReservationCommand, ReservationResponse>
{
    private readonly IReservationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public UpdateReservationCommandHandler(IReservationRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<ReservationResponse> Handle(UpdateReservationCommand request, CancellationToken cancellationToken)
    {
        var r = await _repo.GetByIdAsync(request.Id, _currentUser.CompanyId, cancellationToken)
            ?? throw new Exception("Rezervasiya tapılmadı.");

        if (r.Status == ReservationStatus.Cancelled || r.Status == ReservationStatus.Completed)
            throw new Exception("Ləğv edilmiş və ya tamamlanmış rezervasiyanı dəyişmək olmaz.");

        if (!TimeSpan.TryParse(request.Request.ReservationTime, out var time))
            throw new Exception("Vaxt formatı yanlışdır.");

        var dto = request.Request;
        r.TableId = dto.TableId;
        r.GuestName = dto.GuestName.Trim();
        r.GuestPhone = dto.GuestPhone.Trim();
        r.GuestEmail = dto.GuestEmail?.Trim();
        r.GuestCount = dto.GuestCount;
        r.ReservationDate = dto.ReservationDate.Date;
        r.ReservationTime = time;
        r.DurationMinutes = dto.DurationMinutes > 0 ? dto.DurationMinutes : 90;
        r.Note = dto.Note?.Trim();

        _repo.Update(r);
        await _repo.SaveChangesAsync(cancellationToken);
        return ReservationMapper.ToResponse(r);
    }
}
