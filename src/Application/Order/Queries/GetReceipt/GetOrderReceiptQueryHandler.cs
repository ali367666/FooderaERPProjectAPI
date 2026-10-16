using Application.Common.Exceptions;
using Application.Common.Helpers;
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
    private readonly IOrderPaymentRepository _paymentRepository;

    public GetOrderReceiptQueryHandler(
        IOrderRepository orderRepository,
        ICompanySettingsRepository companySettingsRepository,
        ICurrentUserService currentUserService,
        IOrderPaymentRepository paymentRepository)
    {
        _orderRepository = orderRepository;
        _companySettingsRepository = companySettingsRepository;
        _currentUserService = currentUserService;
        _paymentRepository = paymentRepository;
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
            line.LineTotal = OrderLinePricing.ComputeLineTotal(line);

        var totalAmount = order.Lines
            .DistinctBy(x => x.Id)
            .Where(x => x.Status != OrderLineStatus.Cancelled)
            .Sum(x => x.LineTotal);

        if (request.PaymentId is { } paymentId)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, order.Id, cancellationToken)
                ?? throw new NotFoundException("Payment not found.");
            var allPayments = await _paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            return BuildPaymentReceipt(order, payment, allPayments, payment.IsFiscal ? defaultVatPercent : null, payment.IsFiscal);
        }

        // Only a sale rung through the fiscal register carries VAT on its receipt.
        var isFiscal = order.IsPaid ? order.IsFiscal : request.Fiscal ?? false;
        var lines = BuildLines(order.Lines, groupQuantities, isFiscal ? defaultVatPercent : null, isFiscal);
        var orderPayments = await _paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        var (cash, card, credit, paymentLabel) = SplitByMethod(order, orderPayments);

        return new OrderReceiptResponse
        {
            ReceiptNumber = order.ReceiptNumber ?? $"RCPT-{order.Id}",
            OrderNumber = order.OrderNumber,
            RestaurantName = order.Restaurant?.Name ?? "-",
            RestaurantAddress = order.Restaurant?.Address,
            TableName = order.Table?.Name ?? "-",
            SectionName = order.Table?.Section?.Name,
            ClosedAt = order.PaidAt ?? order.ClosedAt,
            DiscountAmount = order.DiscountAmount,
            ServiceChargeAmount = order.ServiceChargeAmount ?? 0,
            TableRentalAmount = order.TableRentalAmount ?? 0,
            GrandTotal = order.IsPaid
                ? order.TotalAmount
                : Math.Max(0, totalAmount - order.DiscountAmount + (order.ServiceChargeAmount ?? 0) + (order.TableRentalAmount ?? 0)),
            WaiterName = order.Waiter != null ? $"{order.Waiter.FirstName} {order.Waiter.LastName}" : "-",
            OpenedAt = order.OpenedAt,
            PaidAt = order.PaidAt,
            PaymentMethod = paymentLabel,
            TotalAmount = totalAmount,
            CashPaidAmount = cash,
            CardPaidAmount = card,
            CreditPaidAmount = credit,
            PaidAmount = order.PaidAmount,
            ChangeAmount = order.ChangeAmount,
            VatAmount = lines.Sum(x => x.VatAmount),
            IsFiscal = isFiscal,
            Lines = lines
        };
    }

    /// <summary>
    /// Cash / card / credit portions of a settled bill. A bill paid in parts is summed per method
    /// (and labelled "Mixed" when more than one was used); one paid in a single go is all one method.
    /// </summary>
    private static (decimal Cash, decimal Card, decimal Credit, string Label) SplitByMethod(
        Domain.Entities.Order order, List<Domain.Entities.OrderPayment> payments)
    {
        if (payments.Count > 0)
        {
            decimal Sum(PaymentMethod m) =>
                payments.Where(p => p.Method == m).Sum(p => p.Amount + (p.ServiceChargeAmount ?? 0));
            var methods = payments.Select(p => p.Method).Distinct().ToList();
            return (Sum(PaymentMethod.Cash), Sum(PaymentMethod.Card), Sum(PaymentMethod.Credit),
                methods.Count > 1 ? "Mixed" : methods[0].ToString());
        }

        if (!order.IsPaid || order.PaymentMethod is null)
            return (0, 0, 0, order.PaymentMethod?.ToString() ?? "-");

        return order.PaymentMethod switch
        {
            PaymentMethod.Cash => (order.TotalAmount, 0, 0, "Cash"),
            PaymentMethod.Card => (0, order.TotalAmount, 0, "Card"),
            PaymentMethod.Credit => (0, 0, order.TotalAmount, "Credit"),
            _ => (0, 0, 0, order.PaymentMethod.ToString()!)
        };
    }

    /// <summary>One guest's share: their items at the amounts they paid, then their service charge and payment.</summary>
    private static OrderReceiptResponse BuildPaymentReceipt(
        Domain.Entities.Order order,
        Domain.Entities.OrderPayment payment,
        List<Domain.Entities.OrderPayment> allPayments,
        decimal? defaultVatPercent,
        bool isFiscal)
    {
        // Items paid by guests before this one come first, marked "paid"; the total counts only this payment.
        var earlier = allPayments
            .Where(p => p.Id < payment.Id)
            .SelectMany(p => p.Lines)
            .Select(l => new OrderReceiptLineResponse
            {
                MenuItemName = ReceiptName(l.OrderLine.MenuItem.Name, l.OrderLine.IsGift),
                IsGift = l.OrderLine.IsGift,
                MenuCategoryId = l.OrderLine.MenuItem.MenuCategoryId,
                Quantity = l.Quantity,
                UnitPrice = l.OrderLine.UnitPrice,
                LineTotal = l.Amount,
                VatAmount = 0,
                PaidEarlier = true
            });

        var own = payment.Lines
            .Select(l => new OrderReceiptLineResponse
            {
                MenuItemName = ReceiptName(l.OrderLine.MenuItem.Name, l.OrderLine.IsGift),
                IsGift = l.OrderLine.IsGift,
                MenuCategoryId = l.OrderLine.MenuItem.MenuCategoryId,
                Quantity = l.Quantity,
                UnitPrice = l.OrderLine.UnitPrice,
                LineTotal = l.Amount,
                VatAmount = isFiscal ? ComputeVatAmount(l.Amount, l.OrderLine.MenuItem.VatPercent ?? defaultVatPercent) : 0
            })
            .ToList();

        // A payment settled by amount has no items — it shows as one line.
        if (own.Count == 0)
        {
            own.Add(new OrderReceiptLineResponse
            {
                MenuItemName = "Hesabdan ödəniş",
                Quantity = 1,
                UnitPrice = payment.Amount,
                LineTotal = payment.Amount
            });
        }

        var lines = earlier.Concat(own).ToList();
        var service = payment.ServiceChargeAmount ?? 0;

        return new OrderReceiptResponse
        {
            ReceiptNumber = $"RCPT-{order.Id}-P{payment.Id}",
            OrderNumber = order.OrderNumber,
            RestaurantName = order.Restaurant?.Name ?? "-",
            RestaurantAddress = order.Restaurant?.Address,
            TableName = order.Table?.Name ?? "-",
            SectionName = order.Table?.Section?.Name,
            ClosedAt = payment.PaidAtUtc,
            // The order discount is already folded into each line's amount.
            DiscountAmount = 0,
            ServiceChargeAmount = service,
            TableRentalAmount = 0,
            GrandTotal = payment.Amount + service,
            WaiterName = order.Waiter != null ? $"{order.Waiter.FirstName} {order.Waiter.LastName}" : "-",
            OpenedAt = order.OpenedAt,
            PaidAt = payment.PaidAtUtc,
            PaymentMethod = payment.Method.ToString(),
            CashPaidAmount = payment.Method == PaymentMethod.Cash ? payment.Amount + service : 0,
            CardPaidAmount = payment.Method == PaymentMethod.Card ? payment.Amount + service : 0,
            CreditPaidAmount = payment.Method == PaymentMethod.Credit ? payment.Amount + service : 0,
            TotalAmount = payment.Amount,
            PaidAmount = payment.ReceivedAmount,
            ChangeAmount = payment.ChangeAmount,
            VatAmount = lines.Sum(x => x.VatAmount),
            IsFiscal = isFiscal,
            Lines = lines
        };
    }

    /// <summary>The customer receipt says plainly that a gifted item was on the house.</summary>
    private static string ReceiptName(string name, bool isGift) => isGift ? $"{name} (Hədiyyə)" : name;

    private static decimal ComputeVatAmount(decimal lineTotal, decimal? vatPercent)
    {
        if (vatPercent is null || vatPercent <= 0)
            return 0;

        // Prices are VAT-inclusive; back the tax portion out of the total.
        return Math.Round(lineTotal - lineTotal / (1 + vatPercent.Value / 100), 2);
    }

    private static List<OrderReceiptLineResponse> BuildLines(
        IEnumerable<Domain.Entities.OrderLine> orderLines, bool groupQuantities, decimal? defaultVatPercent, bool isFiscal)
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
                    MenuItemName = ReceiptName(x.MenuItem.Name, x.IsGift),
                    IsGift = x.IsGift,
                    MenuCategoryId = x.MenuItem.MenuCategoryId,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    LineTotal = x.LineTotal,
                    VatAmount = isFiscal ? ComputeVatAmount(x.LineTotal, x.MenuItem.VatPercent ?? defaultVatPercent) : 0
                })
                .ToList();
        }

        return activeLines
            .GroupBy(x => new { x.MenuItem.Name, x.MenuItem.MenuCategoryId, x.UnitPrice, VatPercent = x.MenuItem.VatPercent, x.IsGift })
            .Select(g => new OrderReceiptLineResponse
            {
                MenuItemName = ReceiptName(g.Key.Name, g.Key.IsGift),
                IsGift = g.Key.IsGift,
                MenuCategoryId = g.Key.MenuCategoryId,
                Quantity = g.Sum(x => x.Quantity),
                UnitPrice = g.Key.UnitPrice,
                LineTotal = g.Sum(x => x.LineTotal),
                VatAmount = isFiscal ? ComputeVatAmount(g.Sum(x => x.LineTotal), g.Key.VatPercent ?? defaultVatPercent) : 0
            })
            .ToList();
    }
}
