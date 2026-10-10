using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Shift.Dtos;
using MediatR;

namespace Application.Shift.Queries;

public class GetCurrentShiftQueryHandler : IRequestHandler<GetCurrentShiftQuery, ShiftResponse?>
{
    private readonly IShiftRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentShiftQueryHandler(IShiftRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<ShiftResponse?> Handle(GetCurrentShiftQuery request, CancellationToken cancellationToken)
    {
        var shift = await _repository.GetOpenShiftAsync(_currentUserService.CompanyId, request.RestaurantId, cancellationToken);
        if (shift is null) return null;

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
