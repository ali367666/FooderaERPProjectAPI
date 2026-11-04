using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Shift.Dtos;
using MediatR;

namespace Application.Shift.Commands;

public class CloseShiftCommandHandler : IRequestHandler<CloseShiftCommand, ZReportResponse>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ISaleReturnRepository _saleReturnRepository;
    private readonly IOrderPaymentRepository _paymentRepository;
    private readonly ICompanySettingsRepository _settingsRepository;
    private readonly ICurrentUserService _currentUserService;

    public CloseShiftCommandHandler(
        IShiftRepository shiftRepository,
        IOrderRepository orderRepository,
        ISaleReturnRepository saleReturnRepository,
        IOrderPaymentRepository paymentRepository,
        ICompanySettingsRepository settingsRepository,
        ICurrentUserService currentUserService)
    {
        _settingsRepository = settingsRepository;
        _shiftRepository = shiftRepository;
        _orderRepository = orderRepository;
        _saleReturnRepository = saleReturnRepository;
        _paymentRepository = paymentRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ZReportResponse> Handle(CloseShiftCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        // An opening time means the company works by the day, not in shifts: there is nothing to close.
        var settings = await _settingsRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        if (settings?.OpeningTime is not null)
            throw new BadRequestException("Sistem növbə ilə işləmir (açılış vaxtı təyin olunub) — hesabatı bağlamaq olmaz.");

        var shift = await _shiftRepository.GetByIdAsync(request.ShiftId, companyId, cancellationToken);
        if (shift is null)
            throw new Exception("Növbə tapılmadı.");
        if (!shift.IsOpen)
            throw new Exception("Bu növbə artıq bağlanıb.");

        shift.IsOpen = false;
        shift.ClosedByUserId = _currentUserService.UserId;
        shift.ClosedAt = DateTime.UtcNow;
        shift.ClosingCashAmount = request.Request.ClosingCashAmount;

        _shiftRepository.Update(shift);
        await _shiftRepository.SaveChangesAsync(cancellationToken);

        var paidOrders = await _orderRepository.GetPaidBetweenAsync(
            companyId, shift.RestaurantId, shift.OpenedAt, shift.ClosedAt.Value, cancellationToken);

        var returns = await _saleReturnRepository.GetBetweenAsync(
            companyId, shift.RestaurantId, shift.OpenedAt, shift.ClosedAt.Value, cancellationToken);

        var bankBreakdown = await _paymentRepository.GetCardBankTotalsAsync(
            paidOrders.Select(o => o.Id).ToList(), cancellationToken);

        return ZReportBuilder.Build(shift, paidOrders, returns, bankBreakdown);
    }
}
