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

        // A company that works in shifts has a shift only while one is open. It is never started here:
        // the cashier opens the next one (with the counted opening cash) after closing the previous.
        var shift = await _repository.GetOpenShiftAsync(companyId, request.RestaurantId, cancellationToken);
        if (shift is null)
            return null;

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
