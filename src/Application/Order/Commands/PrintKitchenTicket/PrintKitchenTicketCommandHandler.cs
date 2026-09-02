using System.Text;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.PrintKitchenTicket;

public class PrintKitchenTicketCommandHandler : IRequestHandler<PrintKitchenTicketCommand, int>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPrinterRepository _printerRepository;
    private readonly INetworkPrinterService _networkPrinterService;
    private readonly ICurrentUserService _currentUserService;

    public PrintKitchenTicketCommandHandler(
        IOrderRepository orderRepository,
        IPrinterRepository printerRepository,
        INetworkPrinterService networkPrinterService,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _printerRepository = printerRepository;
        _networkPrinterService = networkPrinterService;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(PrintKitchenTicketCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(request.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Sifariş tapılmadı.");

        var printer = await _printerRepository.GetByIdAsync(request.PrinterId, companyId, cancellationToken);
        if (printer is null)
            throw new Exception("Printer tapılmadı.");
        if (!printer.IsActive)
            throw new Exception("Bu printer deaktivdir.");

        var linesToPrint = order.Lines
            .DistinctBy(x => x.Id)
            .Where(x =>
                x.Status != OrderLineStatus.Cancelled
                && x.KitchenPrintedAt == null
                && x.MenuItem.PrinterId == request.PrinterId)
            .OrderBy(x => x.Id)
            .ToList();

        if (linesToPrint.Count == 0)
            return 0;

        var now = DateTime.UtcNow;
        var content = BuildTicketContent(order, linesToPrint, now);

        await _networkPrinterService.PrintAsync(printer.IpAddress, printer.Port, content, cancellationToken);

        foreach (var line in linesToPrint)
            line.KitchenPrintedAt = now;

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return linesToPrint.Count;
    }

    private static string BuildTicketContent(
        Domain.Entities.Order order, List<Domain.Entities.OrderLine> lines, DateTime now)
    {
        var sb = new StringBuilder();
        sb.AppendLine(order.Restaurant?.Name ?? "");
        sb.AppendLine("MƏTBƏX");
        sb.AppendLine(new string('-', 32));
        sb.AppendLine($"Masa: {order.Table?.Name ?? "-"}");
        sb.AppendLine($"Sifariş: {order.OrderNumber}");
        sb.AppendLine($"Vaxt: {now:dd.MM.yyyy HH:mm}");
        if (order.Waiter is not null)
            sb.AppendLine($"Ofisiant: {order.Waiter.FirstName} {order.Waiter.LastName}");
        sb.AppendLine(new string('-', 32));

        foreach (var line in lines)
        {
            var prefix = line.ParentLineId is not null ? "  > " : "";
            sb.AppendLine($"{prefix}{line.Quantity} x {line.MenuItem?.Name ?? "?"}");
            if (!string.IsNullOrWhiteSpace(line.Note))
                sb.AppendLine($"   Qeyd: {line.Note}");
            if (line.HoldUntilUtc is { } holdUntil && holdUntil > now)
                sb.AppendLine($"   GÖZLƏDƏ: {Math.Ceiling((holdUntil - now).TotalMinutes)} dəq sonra");
        }

        sb.AppendLine(new string('-', 32));
        return sb.ToString();
    }
}
