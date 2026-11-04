using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Orders.Services;

public sealed class OrderPaymentService : IOrderPaymentService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderPaymentRepository _paymentRepository;
    private readonly IRecipeStockDeductionService _recipeStockDeductionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICounterpartyRepository _counterpartyRepository;
    private readonly ICompanySettingsRepository _settingsRepository;
    private readonly ILogger<OrderPaymentService> _logger;

    public OrderPaymentService(
        IOrderRepository orderRepository,
        IOrderPaymentRepository paymentRepository,
        IRecipeStockDeductionService recipeStockDeductionService,
        ICurrentUserService currentUserService,
        ICounterpartyRepository counterpartyRepository,
        ICompanySettingsRepository settingsRepository,
        ILogger<OrderPaymentService> logger)
    {
        _settingsRepository = settingsRepository;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _recipeStockDeductionService = recipeStockDeductionService;
        _currentUserService = currentUserService;
        _counterpartyRepository = counterpartyRepository;
        _logger = logger;
    }

    public async Task<PartPaymentResult> PayAsync(
        Domain.Entities.Order order,
        IReadOnlyList<PartPaymentLine>? requested,
        decimal? settleAmount,
        PaymentMethod method,
        decimal receivedAmount,
        bool isFiscal,
        int? bankId,
        CancellationToken cancellationToken,
        PaymentMethod? remainderMethod = null,
        int? remainderBankId = null)
    {
        if (order.IsPaid || order.Status == OrderStatus.Paid)
            throw new BadRequestException("This order is already paid.");
        var kitchenApproval = (await _settingsRepository.GetByCompanyIdAsync(order.CompanyId, cancellationToken))?.KitchenApprovalRequired ?? true;
        if (!OrderPayability.CanPay(order.Status, kitchenApproval))
            throw new BadRequestException(OrderPayability.RefusalMessage(order.Status, kitchenApproval));
        if (order.TableRentalStartedAt is not null && order.TableRentalStoppedAt is null)
            throw new BadRequestException("Masa icarəsi taymeri hələ işləyir — əvvəlcə dayandırın.");

        var previous = await _paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        var lines = OrderPaymentMath.ActiveLines(order);
        var subtotal = OrderPaymentMath.Subtotal(lines);
        var factor = OrderPaymentMath.DiscountFactor(subtotal, order.DiscountAmount);
        var paidQty = OrderPaymentMath.PaidQuantities(previous);
        int Remaining(OrderLine l) => l.Quantity - paidQty.GetValueOrDefault(l.Id);

        var billTotal = OrderPaymentMath.BillTotal(order, subtotal);
        var amountRemaining = Math.Max(0, billTotal - previous.Sum(p => p.Amount));

        // What this payment covers: chosen items, or — "by amount" — just a sum of money (a guest
        // splitting their bill between cash and card, say), with no items attached.
        var selection = new List<(OrderLine Line, int Quantity)>();
        var byAmount = settleAmount is not null;
        if (byAmount)
        {
            if (settleAmount!.Value <= 0)
                throw new BadRequestException("Məbləğ 0-dan böyük olmalıdır.");
            if (amountRemaining <= 0)
                throw new BadRequestException("Ödəniləcək məbləğ qalmayıb.");
        }
        else if (requested is null)
        {
            selection.AddRange(lines.Where(l => Remaining(l) > 0).Select(l => (l, Remaining(l))));
        }
        else
        {
            foreach (var group in requested.GroupBy(r => r.OrderLineId))
            {
                var line = lines.FirstOrDefault(l => l.Id == group.Key)
                    ?? throw new BadRequestException("Seçilmiş məhsul bu sifarişdə tapılmadı.");
                var quantity = group.Sum(r => r.Quantity);
                if (quantity <= 0)
                    throw new BadRequestException("Miqdar 0-dan böyük olmalıdır.");
                if (quantity > Remaining(line))
                    throw new BadRequestException($"\"{line.MenuItem.Name}\" üçün ödənilməmiş miqdar {Remaining(line)}-dir.");
                if (OrderPaymentMath.IsAtomic(line) && quantity != line.Quantity)
                    throw new BadRequestException($"\"{line.MenuItem.Name}\" hissə-hissə ödənilə bilməz.");
                selection.Add((line, quantity));
            }
        }

        decimal amount;
        bool isFinal;
        List<decimal> portions = new();

        if (byAmount)
        {
            amount = Math.Min(Math.Round(settleAmount!.Value, 2, MidpointRounding.AwayFromZero), amountRemaining);
            isFinal = amount >= amountRemaining;
        }
        else
        {
            // Lines that cost nothing (gifts) never hold the order open.
            var coveredQty = selection.ToDictionary(s => s.Line.Id, s => s.Quantity);
            var linesCovered = lines
                .Where(l => l.LineTotal > 0)
                .All(l => Remaining(l) - coveredQty.GetValueOrDefault(l.Id) <= 0);

            if (selection.Count == 0 && !linesCovered)
                throw new BadRequestException("Ödəniş üçün məhsul seçin.");

            portions = selection.Select(s => OrderPaymentMath.Portion(s.Line, s.Quantity, factor)).ToList();
            // Settled by amount earlier? Then the items may be unpaid on paper while nothing is left to pay.
            isFinal = linesCovered || portions.Sum() >= amountRemaining;
            amount = isFinal ? amountRemaining : Math.Min(portions.Sum(), amountRemaining);
            if (!isFinal && amount <= 0)
                throw new BadRequestException("Seçilmiş məhsulların məbləği 0-dır.");

            // Whatever rounding (and the table rental) adds goes on the last line of the final payment.
            if (isFinal && portions.Count > 0)
                portions[^1] += amount - portions.Sum();
        }

        // The service charge recorded on the order is added on the payment that closes it.
        var serviceCharge = isFinal && order.ServiceChargeAmount is > 0
            ? order.ServiceChargeAmount
            : null;
        var due = amount + (serviceCharge ?? 0);

        // Two methods for one share: the first takes what was typed, the rest goes to the other one.
        decimal? primaryPart = null;
        if (remainderMethod is not null && method != PaymentMethod.Credit)
        {
            if (remainderMethod == PaymentMethod.Credit || remainderMethod == method)
                throw new BadRequestException("Qalan məbləğ üçün fərqli ödəniş üsulu (nağd və ya kart) seçin.");
            if (receivedAmount > 0 && receivedAmount < due)
                primaryPart = Math.Round(receivedAmount, 2, MidpointRounding.AwayFromZero);
        }

        var bank = method == PaymentMethod.Card
            ? await PaymentBankResolver.RequireAsync(_counterpartyRepository, order.CompanyId, bankId, cancellationToken)
            : null;
        var remainderBank = primaryPart is not null && remainderMethod == PaymentMethod.Card
            ? await PaymentBankResolver.RequireAsync(_counterpartyRepository, order.CompanyId, remainderBankId, cancellationToken)
            : null;

        decimal received;
        decimal change;
        if (method == PaymentMethod.Credit)
        {
            if (order.CounterpartyId is null || order.Counterparty is null)
                throw new BadRequestException("Borca yazmaq üçün əvvəlcə müştəri seçilməlidir.");
            received = 0;
            change = 0;
        }
        else if (method == PaymentMethod.Card)
        {
            received = due;
            change = 0;
        }
        else
        {
            if (receivedAmount < due && primaryPart is null)
                throw new BadRequestException("Paid amount cannot be less than total amount.");
            received = receivedAmount;
            change = Math.Max(0, receivedAmount - due);
        }

        var paidAt = DateTime.UtcNow;
        var paidBy = _currentUserService.UserId > 0 ? _currentUserService.UserId : (int?)null;

        // The rest of the share, taken with the other method as a payment of its own.
        OrderPayment? remainderPayment = null;
        var primaryAmount = amount;
        if (primaryPart is { } part)
        {
            var remainderMoney = Math.Round(due - part, 2, MidpointRounding.AwayFromZero);
            if (remainderMoney > amount)
                throw new BadRequestException("İlk ödəniş məbləği servis haqqından az ola bilməz.");
            // Cash handed over / the sum put on the card is exactly the typed amount: no change.
            received = part;
            change = 0;
            primaryAmount = amount - remainderMoney;
            remainderPayment = new OrderPayment
            {
                CompanyId = order.CompanyId,
                OrderId = order.Id,
                Method = remainderMethod!.Value,
                BankCounterpartyId = remainderBank?.Id,
                Amount = remainderMoney,
                ReceivedAmount = remainderMoney,
                ChangeAmount = 0,
                IsFiscal = isFiscal,
                PaidAtUtc = paidAt,
                PaidByUserId = paidBy
            };
        }

        var payment = new OrderPayment
        {
            CompanyId = order.CompanyId,
            OrderId = order.Id,
            Method = method,
            BankCounterpartyId = bank?.Id,
            Amount = primaryAmount,
            ServiceChargeAmount = serviceCharge,
            ReceivedAmount = received,
            ChangeAmount = change,
            IsFiscal = isFiscal,
            PaidAtUtc = paidAt,
            PaidByUserId = paidBy
        };
        for (var i = 0; i < selection.Count && i < portions.Count; i++)
        {
            payment.Lines.Add(new OrderPaymentLine
            {
                OrderLineId = selection[i].Line.Id,
                Quantity = selection[i].Quantity,
                Amount = portions[i]
            });
        }

        if (method == PaymentMethod.Credit)
        {
            order.Counterparty!.CurrentDebtAmount += due;
            await _counterpartyRepository.AddDebtEntryAsync(new CounterpartyDebtEntry
            {
                CompanyId = order.CompanyId,
                CounterpartyId = order.Counterparty.Id,
                Type = CounterpartyDebtEntryType.CreditSale,
                Amount = due,
                BalanceAfter = order.Counterparty.CurrentDebtAmount,
                OrderId = order.Id,
                CreatedByUserId = _currentUserService.UserId > 0 ? _currentUserService.UserId : null
            }, cancellationToken);
        }

        await _paymentRepository.AddAsync(payment, cancellationToken);
        if (remainderPayment is not null)
            await _paymentRepository.AddAsync(remainderPayment, cancellationToken);

        if (isFinal)
        {
            var nonKitchenLines = lines
                .Where(x => (x.MenuItem?.PreparationType ?? x.PreparationType) != PreparationType.Kitchen)
                .ToList();
            foreach (var line in nonKitchenLines.Where(x => !x.IsStockDeducted))
                await _recipeStockDeductionService.DeductForOrderLineAsync(line, cancellationToken);

            var all = previous.Append(payment).Concat(remainderPayment is null ? Array.Empty<OrderPayment>() : new[] { remainderPayment }).ToList();
            var totalService = all.Sum(p => p.ServiceChargeAmount ?? 0);
            order.TotalAmount = billTotal + totalService;
            order.ServiceChargeAmount = totalService > 0 ? totalService : null;
            order.IsPaid = true;
            order.PaidAt = payment.PaidAtUtc;
            order.PaymentMethod = method;
            order.BankCounterpartyId = all.Where(p => p.Method == PaymentMethod.Card).Select(p => p.BankCounterpartyId).LastOrDefault();
            order.IsFiscal = all.Any(p => p.IsFiscal);
            order.PaidAmount = all.Where(p => p.Method != PaymentMethod.Credit).Sum(p => p.ReceivedAmount);
            order.ChangeAmount = all.Where(p => p.Method != PaymentMethod.Credit).Sum(p => p.ChangeAmount);
            order.Status = OrderStatus.Paid;
            order.ReceiptNumber = $"RCPT-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{order.Id}";
            order.ClosedAt = order.PaidAt;
            if (order.Table is not null)
                order.Table.IsOccupied = false;
        }

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Part payment {Amount} ({Method}) recorded for order {OrderId}; closed: {Closed}",
            amount, method, order.Id, isFinal);

        return new PartPaymentResult(
            payment, isFinal, isFinal ? 0 : amountRemaining - amount,
            remainderPayment is null ? null : new[] { remainderPayment });
    }
}
