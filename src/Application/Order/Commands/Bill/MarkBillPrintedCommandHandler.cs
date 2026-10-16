using System.Text;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.Bill;

public class MarkBillPrintedCommandHandler : IRequestHandler<MarkBillPrintedCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICompanySettingsRepository _companySettingsRepository;
    private readonly ICurrentUserService _currentUserService;

    public MarkBillPrintedCommandHandler(
        IOrderRepository orderRepository,
        ICompanySettingsRepository companySettingsRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _companySettingsRepository = companySettingsRepository;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(MarkBillPrintedCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var order = await _orderRepository.GetByIdAsync(request.OrderId, companyId, cancellationToken)
            ?? throw new NotFoundException("Sifariş tapılmadı.");

        if (order.Status is OrderStatus.Paid or OrderStatus.Cancelled)
            return order.IsBillLocked;

        var settings = await _companySettingsRepository.GetByCompanyIdAsync(companyId, cancellationToken);

        order.BillPrintedAt = DateTime.UtcNow;
        if (request.Final || settings?.LockOrderAfterBill == true)
            order.IsBillLocked = true;

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return order.IsBillLocked;
    }
}
