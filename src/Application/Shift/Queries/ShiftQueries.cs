using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Shift.Dtos;
using MediatR;

namespace Application.Shift.Queries;

public record GetCurrentShiftQuery(int RestaurantId) : IRequest<ShiftResponse?>;

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

public record GetZReportQuery(int ShiftId) : IRequest<ZReportResponse>;

public class GetZReportQueryHandler : IRequestHandler<GetZReportQuery, ZReportResponse>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetZReportQueryHandler(
        IShiftRepository shiftRepository,
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _shiftRepository = shiftRepository;
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ZReportResponse> Handle(GetZReportQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var shift = await _shiftRepository.GetByIdAsync(request.ShiftId, companyId, cancellationToken);
        if (shift is null)
            throw new Exception("Növbə tapılmadı.");

        var to = shift.ClosedAt ?? DateTime.UtcNow;
        var paidOrders = await _orderRepository.GetPaidBetweenAsync(companyId, shift.RestaurantId, shift.OpenedAt, to, cancellationToken);

        return ZReportBuilder.Build(shift, paidOrders);
    }
}
