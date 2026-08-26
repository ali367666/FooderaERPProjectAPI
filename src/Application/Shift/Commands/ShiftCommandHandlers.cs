using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Shift.Dtos;
using MediatR;

namespace Application.Shift.Commands;

public class OpenShiftCommandHandler : IRequestHandler<OpenShiftCommand, ShiftResponse>
{
    private readonly IShiftRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public OpenShiftCommandHandler(IShiftRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<ShiftResponse> Handle(OpenShiftCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var existingOpen = await _repository.GetOpenShiftAsync(companyId, dto.RestaurantId, cancellationToken);
        if (existingOpen is not null)
            throw new Exception("Bu restoran üçün artıq açıq növbə var.");

        var shift = new Domain.Entities.Shift
        {
            CompanyId = companyId,
            RestaurantId = dto.RestaurantId,
            OpenedByUserId = _currentUserService.UserId,
            OpenedAt = DateTime.UtcNow,
            OpeningCashAmount = dto.OpeningCashAmount,
            IsOpen = true
        };

        await _repository.AddAsync(shift, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

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

public class CloseShiftCommandHandler : IRequestHandler<CloseShiftCommand, ZReportResponse>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public CloseShiftCommandHandler(
        IShiftRepository shiftRepository,
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _shiftRepository = shiftRepository;
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ZReportResponse> Handle(CloseShiftCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

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

        return ZReportBuilder.Build(shift, paidOrders);
    }
}
