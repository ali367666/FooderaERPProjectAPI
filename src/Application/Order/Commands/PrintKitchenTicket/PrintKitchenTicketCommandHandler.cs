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
    private readonly ICompanySettingsRepository _companySettingsRepository;
    private readonly ICurrentUserService _currentUserService;

    public PrintKitchenTicketCommandHandler(
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

    public async Task<int> Handle(PrintKitchenTicketCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(request.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Sifariş tapılmadı.");

        var settings = await _companySettingsRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        var isOnHold = order.HoldUntilUtc is not null;

        if (isOnHold && settings?.PrintKitchenOnHold != true)
            throw new Exception("Sifariş gözlədədir — mətbəxə göndərmək üçün əvvəlcə gözləməni ləğv edin.");

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
                && (x.MenuItem.IsSet ? x.MenuItem.SetPrinterId ?? x.MenuItem.PrinterId : x.MenuItem.PrinterId) == request.PrinterId)
            .OrderBy(x => x.Id)
            .ToList();

        if (linesToPrint.Count == 0)
            return 0;

        var groupQuantities = settings?.PrintKitchenGroupQuantities ?? false;
        var showBusinessName = settings?.PrintKitchenShowBusinessName ?? true;

        var now = DateTime.UtcNow;
        var content = BuildTicketContent(order, linesToPrint, now, groupQuantities, showBusinessName, isOnHold, chiefCopyOf: null);

        await _networkPrinterService.PrintAsync(printer.IpAddress, printer.Port, content, cancellationToken);

        // ChiefPrint — the head chef gets a copy of every station's ticket on the branch's chief printer.
        if (settings?.PrintChiefCopy == true)
        {
            var chief = await _printerRepository.GetChiefAsync(companyId, order.RestaurantId, null, cancellationToken);
            if (chief is not null && chief.IsActive && chief.Id != printer.Id)
            {
                var chiefContent = BuildTicketContent(order, linesToPrint, now, groupQuantities, showBusinessName, isOnHold, chiefCopyOf: printer.Name);
                try
                {
                    await _networkPrinterService.PrintAsync(chief.IpAddress, chief.Port, chiefContent, cancellationToken);
                }
                catch
                {
                    // the station ticket already printed — a failed chief copy must not block the kitchen
                }
            }
        }

        foreach (var line in linesToPrint)
            line.KitchenPrintedAt = now;

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return linesToPrint.Count;
    }

    private static string BuildTicketContent(
        Domain.Entities.Order order,
        List<Domain.Entities.OrderLine> lines,
        DateTime now,
        bool groupQuantities,
        bool showBusinessName,
        bool isOnHold,
        string? chiefCopyOf)
    {
        var sb = new StringBuilder();
        if (showBusinessName)
            sb.AppendLine(order.Restaurant?.Name ?? "");
        sb.AppendLine(chiefCopyOf is null ? "MƏTBƏX" : $"ŞEF NÜSXƏSİ ({chiefCopyOf})");
        if (isOnHold)
            sb.AppendLine("*** GÖZLƏMƏDƏ — HAZIRLAMAYIN ***");
        sb.AppendLine(new string('-', 32));
        sb.AppendLine($"Masa: {order.Table?.Name ?? "-"}");
        sb.AppendLine($"Sifariş: {order.OrderNumber}");
        sb.AppendLine($"Vaxt: {now:dd.MM.yyyy HH:mm}");
        if (order.Waiter is not null)
            sb.AppendLine($"Ofisiant: {order.Waiter.FirstName} {order.Waiter.LastName}");
        sb.AppendLine(new string('-', 32));

        foreach (var line in BuildPrintLines(lines, now, groupQuantities))
        {
            sb.AppendLine($"{line.Prefix}{line.Quantity} x {line.Name}");
            if (!string.IsNullOrWhiteSpace(line.Note))
                sb.AppendLine($"   Qeyd: {line.Note}");
            if (line.HoldRemainingMinutes is { } minutes)
                sb.AppendLine($"   GÖZLƏDƏ: {minutes} dəq sonra");
        }

        sb.AppendLine(new string('-', 32));
        return sb.ToString();
    }

    private record PrintLine(string Prefix, int Quantity, string Name, string? Note, double? HoldRemainingMinutes);

    /// <summary>
    /// SET sub-lines and lines with an active hold timer always print individually (grouping
    /// them would hide which parent/hold they belong to). Everything else can be merged by
    /// (menu item, note) when the kitchen-grouping setting is on.
    /// </summary>
    private static List<PrintLine> BuildPrintLines(
        List<Domain.Entities.OrderLine> lines, DateTime now, bool groupQuantities)
    {
        static double? HoldMinutes(Domain.Entities.OrderLine l, DateTime now) =>
            l.HoldUntilUtc is { } holdUntil && holdUntil > now
                ? Math.Ceiling((holdUntil - now).TotalMinutes)
                : null;

        if (!groupQuantities)
        {
            return lines.Select(l => new PrintLine(
                l.ParentLineId is not null ? "  > " : "",
                l.Quantity,
                l.MenuItem?.Name ?? "?",
                l.Note,
                HoldMinutes(l, now))).ToList();
        }

        var groupable = lines.Where(l => l.ParentLineId is null && HoldMinutes(l, now) is null).ToList();
        var ungroupable = lines.Where(l => l.ParentLineId is not null || HoldMinutes(l, now) is not null).ToList();

        var grouped = groupable
            .GroupBy(l => new { Name = l.MenuItem?.Name ?? "?", l.Note })
            .Select(g => new PrintLine("", g.Sum(x => x.Quantity), g.Key.Name, g.Key.Note, null));

        var individual = ungroupable.Select(l => new PrintLine(
            l.ParentLineId is not null ? "  > " : "",
            l.Quantity,
            l.MenuItem?.Name ?? "?",
            l.Note,
            HoldMinutes(l, now)));

        return grouped.Concat(individual).ToList();
    }
}
