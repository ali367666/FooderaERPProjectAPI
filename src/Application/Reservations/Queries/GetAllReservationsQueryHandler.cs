using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reservations.Dtos;
using MediatR;

namespace Application.Reservations.Queries;

public class GetAllReservationsQueryHandler
    : IRequestHandler<GetAllReservationsQuery, List<ReservationResponse>>
{
    private readonly IReservationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetAllReservationsQueryHandler(IReservationRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<ReservationResponse>> Handle(GetAllReservationsQuery request, CancellationToken cancellationToken)
    {
        // A date that carries a time (2026-11-04T14:30) is that time; the explicit fromTime / toTime win.
        var from = request.FromTime ?? TimeOfDayOrNull(request.DateFrom ?? request.Date);
        var to = request.ToTime ?? TimeOfDayOrNull(request.DateTo ?? request.Date);
        foreach (var time in new[] { from, to })
            if (time is { } v && (v < TimeSpan.Zero || v >= TimeSpan.FromDays(1)))
                throw new Application.Common.Exceptions.BadRequestException("Saat 00:00 ilə 23:59 arasında olmalıdır.");
        var firstDay = (request.DateFrom ?? request.Date)?.Date;
        var lastDay = (request.DateTo ?? request.Date)?.Date;
        if (from is { } f && to is { } t && firstDay == lastDay && t + TimeSpan.FromMinutes(1) <= f)
            throw new Application.Common.Exceptions.BadRequestException("Bitmə vaxtı başlanğıc vaxtından sonra olmalıdır.");

        var list = await _repo.GetAllAsync(_currentUser.CompanyId, request.Date, request.DateFrom, request.DateTo, from, to, cancellationToken);
        return list.Select(ReservationMapper.ToResponse).ToList();
    }

    private static TimeSpan? TimeOfDayOrNull(DateTime? value)
        => value is { } v && v.TimeOfDay != TimeSpan.Zero ? v.TimeOfDay : null;
}
