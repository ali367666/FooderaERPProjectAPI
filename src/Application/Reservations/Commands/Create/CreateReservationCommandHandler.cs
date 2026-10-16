using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reservations.Dtos;
using Domain.Entities;
using MediatR;

namespace Application.Reservations.Commands.Create;

public class CreateReservationCommandHandler
    : IRequestHandler<CreateReservationCommand, ReservationResponse>
{
    private readonly IReservationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public CreateReservationCommandHandler(
        IReservationRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<ReservationResponse> Handle(
        CreateReservationCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Request;
        if (!TimeSpan.TryParse(dto.ReservationTime, out var time))
            throw new Exception("Vaxt formatı yanlışdır. HH:mm formatında daxil edin.");

        var duration = dto.DurationMinutes > 0 ? dto.DurationMinutes : 90;
        if (dto.TableId is int tableId)
        {
            var clash = await _repo.FindOverlappingAsync(
                _currentUser.CompanyId, tableId, dto.ReservationDate.Date + time, duration, null, cancellationToken);
            if (clash is not null)
                throw new BadRequestException(
                    $"Bu masa həmin vaxtda artıq rezerv olunub ({clash.ReservationTime:hh\\:mm}, {clash.GuestName}).");
        }

        var reservation = new Reservation
        {
            CompanyId = _currentUser.CompanyId,
            RestaurantId = dto.RestaurantId,
            TableId = dto.TableId,
            GuestName = dto.GuestName.Trim(),
            GuestPhone = dto.GuestPhone.Trim(),
            GuestEmail = dto.GuestEmail?.Trim(),
            GuestCount = dto.GuestCount,
            ReservationDate = dto.ReservationDate.Date,
            ReservationTime = time,
            DurationMinutes = duration,
            Note = dto.Note?.Trim(),
        };

        await _repo.AddAsync(reservation, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);

        var created = await _repo.GetByIdAsync(reservation.Id, _currentUser.CompanyId, cancellationToken)
            ?? reservation;
        return ReservationMapper.ToResponse(created);
    }
}
