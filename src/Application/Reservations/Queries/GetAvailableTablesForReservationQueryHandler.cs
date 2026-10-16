using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.RestaurantTables.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Reservations.Queries;

public class GetAvailableTablesForReservationQueryHandler
    : IRequestHandler<GetAvailableTablesForReservationQuery, List<RestaurantTableResponse>>
{
    private readonly IReservationRepository _repo;
    private readonly IRestaurantTableRepository _tableRepo;
    private readonly ICurrentUserService _currentUser;

    public GetAvailableTablesForReservationQueryHandler(
        IReservationRepository repo, IRestaurantTableRepository tableRepo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _tableRepo = tableRepo;
        _currentUser = currentUser;
    }

    public async Task<List<RestaurantTableResponse>> Handle(
        GetAvailableTablesForReservationQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId;
        var r = await _repo.GetByIdAsync(request.ReservationId, companyId, cancellationToken)
            ?? throw new BadRequestException("Rezervasiya tapılmadı.");

        var start = r.ReservationDate.Date + r.ReservationTime;
        var tables = await _tableRepo.GetAllByRestaurantAsync(companyId, r.RestaurantId, cancellationToken);

        var result = new List<RestaurantTableResponse>();
        foreach (var t in tables)
        {
            if (!t.IsActive || t.Id == r.TableId
                || t.Type is RestaurantTableType.Delivery or RestaurantTableType.TakeAway)
                continue;

            var clash = await _repo.FindOverlappingAsync(
                companyId, t.Id, start, r.DurationMinutes, r.Id, cancellationToken);
            if (clash is not null)
                continue;

            result.Add(new RestaurantTableResponse
            {
                Id = t.Id,
                RestaurantId = t.RestaurantId,
                RestaurantName = t.Restaurant?.Name ?? string.Empty,
                Name = t.Name,
                Capacity = t.Capacity,
                IsActive = t.IsActive,
                IsOccupied = t.IsOccupied,
                PosX = t.PosX,
                PosY = t.PosY,
                Width = t.Width,
                Height = t.Height,
                Shape = t.Shape,
                Rotation = t.Rotation,
                SectionId = t.SectionId,
                HourlyRate = t.HourlyRate,
                Note = t.Note,
                Type = t.Type,
            });
        }

        return result;
    }
}
