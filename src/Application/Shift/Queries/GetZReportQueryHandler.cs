using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Shift.Dtos;
using MediatR;

namespace Application.Shift.Queries;

public class GetZReportQueryHandler : IRequestHandler<GetZReportQuery, ZReportResponse>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ISaleReturnRepository _saleReturnRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetZReportQueryHandler(
        IShiftRepository shiftRepository,
        IOrderRepository orderRepository,
        ISaleReturnRepository saleReturnRepository,
        ICurrentUserService currentUserService)
    {
        _shiftRepository = shiftRepository;
        _orderRepository = orderRepository;
        _saleReturnRepository = saleReturnRepository;
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

        var returns = await _saleReturnRepository.GetBetweenAsync(
            companyId, shift.RestaurantId, shift.OpenedAt, to, cancellationToken);

        return ZReportBuilder.Build(shift, paidOrders, returns);
    }
}
