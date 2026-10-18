using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Shift.Dtos;
using MediatR;

namespace Application.Shift.Queries;

public class GetCurrentShiftQueryHandler : IRequestHandler<GetCurrentShiftQuery, ShiftResponse?>
{
    private readonly IShiftRepository _repository;
    private readonly ICompanySettingsRepository _settingsRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentShiftQueryHandler(
        IShiftRepository repository,
        ICompanySettingsRepository settingsRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _settingsRepository = settingsRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ShiftResponse?> Handle(GetCurrentShiftQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        // An opening time means the company runs on opening hours, not on shifts: the shift stays closed.
        var settings = await _settingsRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        if (settings?.OpeningTime is not null)
            return null;

        // No opening time: the shift is always open — started on first use when none is running.
        var shift = await _repository.GetOpenShiftAsync(companyId, request.RestaurantId, cancellationToken);
        if (shift is null)
        {
            shift = new Domain.Entities.Shift
            {
                CompanyId = companyId,
                RestaurantId = request.RestaurantId,
                OpenedByUserId = _currentUserService.UserId,
                OpenedAt = DateTime.UtcNow,
                OpeningCashAmount = 0,
                IsOpen = true
            };
            await _repository.AddAsync(shift, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return new ShiftResponse
        {
            Id = shift.Id,
            RestaurantId = shift.RestaurantId,
            OpenedByUserId = shift.OpenedByUserId,
            OpenedAt = shift.OpenedAt,
            OpeningCashAmount = shift.OpeningCashAmount,
            IsOpen = shift.IsOpen
        };
    }
}
