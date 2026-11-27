using System.Text.RegularExpressions;
using Application.Banquets.Dtos;
using Application.Banquets.Services;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.Banquets.Queries;

public class GetBanquetTablesQueryHandler : IRequestHandler<GetBanquetTablesQuery, List<BanquetTableResponse>>
{
    private readonly IRestaurantTableRepository _tableRepository;
    private readonly IBanquetTableChecker _checker;
    private readonly ICurrentUserService _currentUserService;

    public GetBanquetTablesQueryHandler(
        IRestaurantTableRepository tableRepository,
        IBanquetTableChecker checker,
        ICurrentUserService currentUserService)
    {
        _tableRepository = tableRepository;
        _checker = checker;
        _currentUserService = currentUserService;
    }

    public async Task<List<BanquetTableResponse>> Handle(GetBanquetTablesQuery request, CancellationToken cancellationToken)
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

        var tables = (await _tableRepository.GetForSearchAsync(companyId, request.RestaurantId, cancellationToken))
            .Where(t => t.IsActive
                && t.Type is not (RestaurantTableType.Delivery or RestaurantTableType.TakeAway)
                && (request.SectionId is null || t.SectionId == request.SectionId))
            .OrderBy(t => Regex.Replace(SearchText.Fold(t.Name), @"\d+", m => m.Value.PadLeft(8, '0')), StringComparer.Ordinal)
            .ToList();

        var result = new List<BanquetTableResponse>();
        foreach (var table in tables)
        {
            var reason = start is { } s
                ? await _checker.GetBookedReasonAsync(companyId, table, s, duration, cancellationToken)
                : await _checker.GetBusyReasonNowAsync(companyId, table, cancellationToken);

            result.Add(new BanquetTableResponse
            {
                Id = table.Id,
                Name = table.Name,
                Capacity = table.Capacity,
                SectionId = table.SectionId,
                SectionName = table.Section?.Name,
                IsAvailable = reason is null,
                BusyReason = reason
            });
        }

        return result;
    }
}
