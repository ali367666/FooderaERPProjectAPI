using Application.Banquets.Dtos;
using Application.Banquets.Services;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.Banquets.Queries;

public class GetBanquetHallsQueryHandler : IRequestHandler<GetBanquetHallsQuery, List<BanquetHallResponse>>
{
    private readonly IRestaurantSectionRepository _sectionRepository;
    private readonly IRestaurantTableRepository _tableRepository;
    private readonly IBanquetTableChecker _checker;
    private readonly ICurrentUserService _currentUserService;

    public GetBanquetHallsQueryHandler(
        IRestaurantSectionRepository sectionRepository,
        IRestaurantTableRepository tableRepository,
        IBanquetTableChecker checker,
        ICurrentUserService currentUserService)
    {
        _sectionRepository = sectionRepository;
        _tableRepository = tableRepository;
        _checker = checker;
        _currentUserService = currentUserService;
    }

    public async Task<List<BanquetHallResponse>> Handle(GetBanquetHallsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        DateTime? start = null;
        var duration = request.DurationMinutes is > 0 ? request.DurationMinutes.Value : 240;
        if (request.Date is { } date)
        {
            if (!TimeSpan.TryParse(request.Time, out var time))
                throw new BadRequestException("Vaxt formatı yanlışdır. HH:mm formatında daxil edin.");
            start = date.Date + time;
        }

        var halls = (await _sectionRepository.GetAllByRestaurantAsync(companyId, request.RestaurantId, cancellationToken))
            .Where(s => s.IsActive && s.IsBanquet)
            .OrderBy(s => s.Name)
            .ToList();

        var tablesBySection = (await _tableRepository.GetForSearchAsync(companyId, request.RestaurantId, cancellationToken))
            .Where(t => t.IsActive
                && t.SectionId is not null
                && t.Type is not (RestaurantTableType.Delivery or RestaurantTableType.TakeAway))
            .ToLookup(t => t.SectionId!.Value);

        var result = new List<BanquetHallResponse>();
        foreach (var hall in halls)
        {
            var tables = tablesBySection[hall.Id].ToList();
            if (tables.Count == 0)
                continue;

            string? busyReason = null;
            foreach (var table in tables)
            {
                var reason = start is { } s
                    ? await _checker.GetBookedReasonAsync(companyId, table, s, duration, cancellationToken)
                    : await _checker.GetBusyReasonNowAsync(companyId, table, cancellationToken);
                if (reason is not null)
                {
                    busyReason = $"{table.Name}: {reason}";
                    break;
                }
            }

            result.Add(new BanquetHallResponse
            {
                Id = hall.Id,
                Name = hall.Name,
                TableCount = tables.Count,
                Capacity = tables.Sum(t => t.Capacity),
                IsAvailable = busyReason is null,
                BusyReason = busyReason
            });
        }

        return result;
    }
}
