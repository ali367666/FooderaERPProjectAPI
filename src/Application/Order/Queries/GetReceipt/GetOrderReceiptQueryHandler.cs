using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Queries.GetReceipt;

public class GetOrderReceiptQueryHandler : IRequestHandler<GetOrderReceiptQuery, OrderReceiptResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICompanySettingsRepository _companySettingsRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetOrderReceiptQueryHandler(
        IOrderRepository orderRepository,
        ICompanySettingsRepository companySettingsRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _companySettingsRepository = companySettingsRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OrderReceiptResponse> Handle(GetOrderReceiptQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, _currentUserService.CompanyId, cancellationToken);
        if (order is null)
            throw new NotFoundException("Order not found.");

        var settings = await _companySettingsRepository.GetByCompanyIdAsync(_currentUserService.CompanyId, cancellationToken);
        var groupQuantities = settings?.PrintGroupQuantities ?? true;
        var defaultVatPercent = settings?.DefaultVatPercent;

        foreach (var line in order.Lines)
            line.LineTotal = line.UnitPrice * line.Quantity;

        var totalAmount = order.Lines
            .DistinctBy(x => x.Id)
            .Where(x => x.Status != OrderLineStatus.Cancelled)
            .Sum(x => x.LineTotal);

        var lines = BuildLines(order.Lines, groupQuantities, defaultVatPercent);

        return new OrderReceiptResponse
        {
            ReceiptNumber = order.ReceiptNumber ?? $"RCPT-{order.Id}",
            OrderNumber = order.OrderNumber,
            RestaurantName = order.Restaurant?.Name ?? "-",
            TableName = order.Table?.Name ?? "-",
            WaiterName = order.Waiter != null ? $"{order.Waiter.FirstName} {order.Waiter.LastName}" : "-",
            OpenedAt = order.OpenedAt,
            PaidAt = order.PaidAt,
            PaymentMethod = order.PaymentMethod?.ToString() ?? "-",
            TotalAmount = totalAmount,
            PaidAmount = order.PaidAmount,
            ChangeAmount = order.ChangeAmount,
            VatAmount = lines.Sum(x => x.VatAmount),
            Lines = lines
        };
    }

    private static decimal ComputeVatAmount(decimal lineTotal, decimal? vatPercent)
    {
        if (vatPercent is null || vatPercent <= 0)
            return 0;

        // Prices are VAT-inclusive; back the tax portion out of the total.
        return Math.Round(lineTotal - lineTotal / (1 + vatPercent.Value / 100), 2);
    }

    private static List<OrderReceiptLineResponse> BuildLines(
        IEnumerable<Domain.Entities.OrderLine> orderLines, bool groupQuantities, decimal? defaultVatPercent)
    {
        var activeLines = orderLines
            .DistinctBy(x => x.Id)
            .Where(x => x.Status != OrderLineStatus.Cancelled)
            .ToList();

        if (!groupQuantities)
        {
            return activeLines
                .Select(x => new OrderReceiptLineResponse
                {
                    MenuItemName = x.MenuItem.Name,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    LineTotal = x.LineTotal,
                    VatAmount = ComputeVatAmount(x.LineTotal, x.MenuItem.VatPercent ?? defaultVatPercent)
                })
                .ToList();
        }

        return activeLines
            .GroupBy(x => new { x.MenuItem.Name, x.UnitPrice, VatPercent = x.MenuItem.VatPercent })
            .Select(g => new OrderReceiptLineResponse
            {
                MenuItemName = g.Key.Name,
                Quantity = g.Sum(x => x.Quantity),
                UnitPrice = g.Key.UnitPrice,
                LineTotal = g.Sum(x => x.LineTotal),
                VatAmount = ComputeVatAmount(g.Sum(x => x.LineTotal), g.Key.VatPercent ?? defaultVatPercent)
            })
            .ToList();
    }
}
