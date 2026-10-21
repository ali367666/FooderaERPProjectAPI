using Application.Banquets.Dtos;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Banquets.Commands;

public class OpenBanquetReservationCommandHandler
    : IRequestHandler<OpenBanquetReservationCommand, OrderResponse>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public OpenBanquetReservationCommandHandler(
        IReservationRepository reservationRepository,
        ICurrentUserService currentUserService,
        IMediator mediator)
    {
        _reservationRepository = reservationRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    public async Task<OrderResponse> Handle(OpenBanquetReservationCommand command, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var reservation = await _reservationRepository.GetByIdAsync(command.ReservationId, companyId, cancellationToken)
            ?? throw new NotFoundException("Rezervasiya tapılmadı.");

        if (!reservation.IsBanquet)
            throw new BadRequestException("Bu, banket rezervasiyası deyil.");
        if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            throw new BadRequestException("Yalnız aktiv banket rezervasiyasını açmaq olar.");

        var tableIds = reservation.BanquetTables.Select(b => b.TableId).ToList();
        if (reservation.TableId is int mainTableId && !tableIds.Contains(mainTableId))
            tableIds.Insert(0, mainTableId);

        // The booking stops holding its own tables once the guests sit down; if opening fails
        // (a table is still busy) it goes back to what it was.
        var previousStatus = reservation.Status;
        reservation.Status = ReservationStatus.Seated;
        _reservationRepository.Update(reservation);
        await _reservationRepository.SaveChangesAsync(cancellationToken);

        try
        {
            var note = $"Banket: {reservation.GuestName} ({reservation.GuestPhone})"
                + (string.IsNullOrWhiteSpace(reservation.Note) ? string.Empty : $" — {reservation.Note}");

            return await _mediator.Send(new OpenBanquetCommand(new OpenBanquetRequest
            {
                RestaurantId = reservation.RestaurantId,
                WaiterId = command.WaiterId,
                TableIds = tableIds,
                GuestCount = reservation.GuestCount,
                Note = note
            }), cancellationToken);
        }
        catch
        {
            reservation.Status = previousStatus;
            _reservationRepository.Update(reservation);
            await _reservationRepository.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }
}
