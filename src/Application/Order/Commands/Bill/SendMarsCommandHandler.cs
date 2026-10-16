using System.Text;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.Bill;

public class SendMarsCommandHandler : IRequestHandler<SendMarsCommand, int>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPrinterRepository _printerRepository;
    private readonly INetworkPrinterService _networkPrinterService;
    private readonly ICompanySettingsRepository _companySettingsRepository;
    private readonly ICurrentUserService _currentUserService;

    public SendMarsCommandHandler(
        IOrderRepository orderRepository,
        IPrinterRepository printerRepository,
        INetworkPrinterService networkPrinterService,
        ICompanySettingsRepository companySettingsRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _printerRepository = printerRepository;
        _networkPrinterService = networkPrinterService;
        _companySettingsRepository = companySettingsRepository;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(SendMarsCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var settings = await _companySettingsRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        if (settings?.PosMarsEnabled != true)
            throw new BadRequestException("Marş funksiyası tənzimləmələrdə aktiv deyil.");

        var order = await _orderRepository.GetByIdAsync(request.OrderId, companyId, cancellationToken)
            ?? throw new NotFoundException("Sifariş tapılmadı.");

        if (order.Status is OrderStatus.Paid or OrderStatus.Cancelled)
            throw new BadRequestException("Bu sifariş üçün marş vermək olmaz.");

        // Marş releases every hold — the whole order is to be prepared now.
        order.HoldUntilUtc = null;
        foreach (var line in order.Lines)
            line.HoldUntilUtc = null;

        // Only what the kitchen already has and hasn't finished yet.
        var pendingByPrinter = order.Lines
            .DistinctBy(x => x.Id)
            .Where(x => x.KitchenPrintedAt != null
                && x.Status is OrderLineStatus.Pending or OrderLineStatus.InPreparation)
            .Select(x => new
            {
                Line = x,
                PrinterId = x.MenuItem.IsSet ? x.MenuItem.SetPrinterId ?? x.MenuItem.PrinterId : x.MenuItem.PrinterId
            })
            .Where(x => x.PrinterId != null)
            .GroupBy(x => x.PrinterId!.Value)
            .ToList();

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        if (pendingByPrinter.Count == 0)
            throw new BadRequestException("Mətbəxə göndərilmiş və hazırlanmamış məhsul yoxdur.");

        var printed = 0;
        foreach (var group in pendingByPrinter)
        {
            var printer = await _printerRepository.GetByIdAsync(group.Key, companyId, cancellationToken);
            if (printer is null || !printer.IsActive)
                continue;

            var sb = new StringBuilder();
            if (settings.PrintKitchenShowBusinessName)
                sb.AppendLine(order.Restaurant?.Name ?? "");
            sb.AppendLine("*** MARŞ ***");
            sb.AppendLine("İNDİ HAZIRLAYIN");
            sb.AppendLine(new string('-', 32));
            sb.AppendLine($"Masa: {order.Table?.Name ?? "-"}");
            sb.AppendLine($"Sifariş: {order.OrderNumber}");
            sb.AppendLine($"Vaxt: {BusinessTime.Now:dd.MM.yyyy HH:mm}");
            if (order.Waiter is not null)
                sb.AppendLine($"Ofisiant: {order.Waiter.FirstName} {order.Waiter.LastName}");
            sb.AppendLine(new string('-', 32));
            foreach (var item in group.OrderBy(x => x.Line.Id))
                sb.AppendLine($"{(item.Line.ParentLineId is not null ? "  > " : "")}{item.Line.Quantity} x {item.Line.MenuItem?.Name ?? "?"}");
            sb.AppendLine(new string('-', 32));

            await _networkPrinterService.PrintAsync(printer.IpAddress, printer.Port, sb.ToString(), cancellationToken);
            printed++;
        }

        return printed;
    }
}
