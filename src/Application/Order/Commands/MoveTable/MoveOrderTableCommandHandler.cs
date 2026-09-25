using System.Text;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.MoveTable;

public class MoveOrderTableCommandHandler : IRequestHandler<MoveOrderTableCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantTableRepository _tableRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly ICompanySettingsRepository _companySettingsRepository;
    private readonly IPrinterRepository _printerRepository;
    private readonly INetworkPrinterService _networkPrinterService;

    public MoveOrderTableCommandHandler(
        IOrderRepository orderRepository,
        IRestaurantTableRepository tableRepository,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        ICompanySettingsRepository companySettingsRepository,
        IPrinterRepository printerRepository,
        INetworkPrinterService networkPrinterService)
    {
        _orderRepository = orderRepository;
        _tableRepository = tableRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _companySettingsRepository = companySettingsRepository;
        _printerRepository = printerRepository;
        _networkPrinterService = networkPrinterService;
    }

    public async Task<OrderResponse> Handle(MoveOrderTableCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var order = await _orderRepository.GetByIdAsync(request.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new Exception("Order not found.");

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new Exception("This order can no longer be moved.");

        if (order.TableId == request.NewTableId)
            throw new Exception("This order is already on that table.");

        var newTable = await _tableRepository.GetByIdAsync(request.NewTableId, companyId, cancellationToken);
        if (newTable is null || newTable.RestaurantId != order.RestaurantId)
            throw new Exception("Table not found for this branch.");

        if (newTable.IsOccupied)
            throw new Exception("The selected table is already occupied.");

        var oldTable = await _tableRepository.GetByIdAsync(order.TableId, companyId, cancellationToken);
        var oldTableId = order.TableId;

        order.TableId = newTable.Id;
        newTable.IsOccupied = true;
        _tableRepository.Update(newTable);

        if (oldTable is not null)
        {
            oldTable.IsOccupied = false;
            _tableRepository.Update(oldTable);
        }

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        await PrintTransferDocumentAsync(order, oldTable?.Name, newTable.Name, companyId, cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Order",
                    EntityId = order.Id.ToString(),
                    ActionType = "MoveTable",
                    Message = $"Order {order.Id} masası dəyişdirildi: {oldTableId} -> {newTable.Id}",
                    IsSuccess = true
                },
                cancellationToken);
        }
        catch
        {
            // audit log failures must not block the operation
        }

        var updatedOrder = await _orderRepository.GetByIdAsync(order.Id, companyId, cancellationToken);
        if (updatedOrder is null)
            throw new Exception("Updated order not found.");

        return new OrderResponse
        {
            Id = updatedOrder.Id,
            OrderNumber = updatedOrder.OrderNumber,
            RestaurantId = updatedOrder.RestaurantId,
            TableId = updatedOrder.TableId,
            TableName = updatedOrder.Table?.Name,
            WaiterId = updatedOrder.WaiterId,
            WaiterName = updatedOrder.Waiter != null ? $"{updatedOrder.Waiter.FirstName} {updatedOrder.Waiter.LastName}" : null,
            Status = updatedOrder.Status.ToString(),
            Note = updatedOrder.Note,
            GuestCount = updatedOrder.GuestCount,
            CounterpartyId = updatedOrder.CounterpartyId,
            CounterpartyName = updatedOrder.Counterparty?.Name,
            CounterpartyDebtAmount = updatedOrder.Counterparty?.CurrentDebtAmount,
            OpenedAt = updatedOrder.OpenedAt,
            ClosedAt = updatedOrder.ClosedAt,
            TotalAmount = updatedOrder.TotalAmount,
            DiscountCode = updatedOrder.DiscountCode,
            DiscountAmount = updatedOrder.DiscountAmount,
            TableHourlyRate = updatedOrder.Table?.HourlyRate,
            TableRentalStartedAt = updatedOrder.TableRentalStartedAt,
            TableRentalStoppedAt = updatedOrder.TableRentalStoppedAt,
            TableRentalAmount = updatedOrder.TableRentalAmount,
            HoldUntilUtc = updatedOrder.HoldUntilUtc,
            IsDelivery = updatedOrder.IsDelivery,
            DeliveryAddress = updatedOrder.DeliveryAddress,
            DeliveryPhone = updatedOrder.DeliveryPhone,
            DeliveryDriverEmployeeId = updatedOrder.DeliveryDriverEmployeeId,
            DeliveryDriverName = updatedOrder.DeliveryDriverEmployee != null ? $"{updatedOrder.DeliveryDriverEmployee.FirstName} {updatedOrder.DeliveryDriverEmployee.LastName}" : null,
            Lines = updatedOrder.Lines.Select(x => new OrderLineResponse
            {
                Id = x.Id,
                MenuItemId = x.MenuItemId,
                MenuItemName = x.MenuItem.Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal,
                HoldUntilUtc = x.HoldUntilUtc,
                KitchenPrintedAt = x.KitchenPrintedAt,
                TimeBasedStartedAt = x.TimeBasedStartedAt,
                TimeBasedStoppedAt = x.TimeBasedStoppedAt,
                IsTimeBased = x.MenuItem.IsTimeBased,
                IsWeightBased = Application.Common.Helpers.OrderLinePricing.IsWeightBased(x.MenuItem.UnitId),
                IsGift = x.IsGift,
                DiscountAmount = x.DiscountAmount,
                Note = x.Note,
                Status = x.Status.ToString()
            }).ToList()
        };
    }

    /// <summary>
    /// Köçürmə sənədi — tells every kitchen station that already received this order's lines that
    /// the order now belongs to a different table. Printing failures never undo the move.
    /// </summary>
    private async Task PrintTransferDocumentAsync(
        Domain.Entities.Order order, string? oldTableName, string newTableName, int companyId, CancellationToken cancellationToken)
    {
        var settings = await _companySettingsRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        if (settings?.PrintTransferDocAuto != true)
            return;

        var printerIds = order.Lines
            .Where(x => x.Status != OrderLineStatus.Cancelled && x.KitchenPrintedAt != null)
            .Select(x => x.MenuItem.IsSet ? x.MenuItem.SetPrinterId ?? x.MenuItem.PrinterId : x.MenuItem.PrinterId)
            .Where(id => id != null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (printerIds.Count == 0)
            return;

        var sb = new StringBuilder();
        if (settings.PrintKitchenShowBusinessName)
            sb.AppendLine(order.Restaurant?.Name ?? "");
        sb.AppendLine("MASA KÖÇÜRMƏ");
        sb.AppendLine(new string('-', 32));
        sb.AppendLine($"Sifariş: {order.OrderNumber}");
        sb.AppendLine($"Köhnə masa: {oldTableName ?? "-"}");
        sb.AppendLine($"Yeni masa: {newTableName}");
        sb.AppendLine($"Vaxt: {BusinessTime.Now:dd.MM.yyyy HH:mm}");
        if (order.Waiter is not null)
            sb.AppendLine($"Ofisiant: {order.Waiter.FirstName} {order.Waiter.LastName}");
        sb.AppendLine(new string('-', 32));
        var content = sb.ToString();

        var copies = settings.PrintTransferDocDouble ? 2 : 1;

        foreach (var printerId in printerIds)
        {
            var printer = await _printerRepository.GetByIdAsync(printerId, companyId, cancellationToken);
            if (printer is null || !printer.IsActive)
                continue;

            for (var i = 0; i < copies; i++)
            {
                try
                {
                    await _networkPrinterService.PrintAsync(printer.IpAddress, printer.Port, content, cancellationToken);
                }
                catch
                {
                    // printer offline — the table move itself already succeeded
                    break;
                }
            }
        }
    }
}
