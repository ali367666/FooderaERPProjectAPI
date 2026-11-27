using Application.Banquets.Services;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reservations.Dtos;
using Domain.Entities;
using MediatR;

namespace Application.Banquets.Commands;

public class CreateBanquetReservationCommandHandler
    : IRequestHandler<CreateBanquetReservationCommand, ReservationResponse>
{
    private readonly IBanquetTableChecker _checker;
    private readonly IReservationRepository _reservationRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateBanquetReservationCommandHandler(
        IBanquetTableChecker checker,
        IReservationRepository reservationRepository,
        ICurrentUserService currentUserService)
    {
        _checker = checker;
        _reservationRepository = reservationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ReservationResponse> Handle(
        CreateBanquetReservationCommand command, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = command.Request;

        if (string.IsNullOrWhiteSpace(dto.GuestName) || string.IsNullOrWhiteSpace(dto.GuestPhone))
            throw new BadRequestException("Müştərinin adı və telefonu məcburidir.");
        if (dto.GuestCount <= 0)
            throw new BadRequestException("Qonaq sayı 0-dan böyük olmalıdır.");
        if (!TimeSpan.TryParse(dto.ReservationTime, out var time))
            throw new BadRequestException("Vaxt formatı yanlışdır. HH:mm formatında daxil edin.");

        var duration = dto.DurationMinutes > 0 ? dto.DurationMinutes : 240;
        var start = dto.ReservationDate.Date + time;

        var tables = dto.SectionId is int sectionId
            ? await _checker.LoadHallTablesAsync(companyId, dto.RestaurantId, sectionId, cancellationToken)
            : await _checker.LoadTablesAsync(companyId, dto.RestaurantId, dto.TableIds, cancellationToken);
        foreach (var table in tables)
        {
            var booked = await _checker.GetBookedReasonAsync(companyId, table, start, duration, cancellationToken);
            if (booked is not null)
                throw new BadRequestException($"{table.Name}: {booked}");
        }

        var reservation = new Reservation
        {
            CompanyId = companyId,
            RestaurantId = dto.RestaurantId,
            TableId = tables[0].Id,
            GuestName = dto.GuestName.Trim(),
            GuestPhone = dto.GuestPhone.Trim(),
            GuestCount = dto.GuestCount,
            ReservationDate = dto.ReservationDate.Date,
            ReservationTime = time,
            DurationMinutes = duration,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
            IsBanquet = true
        };
        foreach (var table in tables)
            reservation.BanquetTables.Add(new ReservationTable { CompanyId = companyId, TableId = table.Id });

        await _reservationRepository.AddAsync(reservation, cancellationToken);
        await _reservationRepository.SaveChangesAsync(cancellationToken);

        var created = await _reservationRepository.GetByIdAsync(reservation.Id, companyId, cancellationToken)
            ?? reservation;
        return ReservationMapper.ToResponse(created);
    }
}
