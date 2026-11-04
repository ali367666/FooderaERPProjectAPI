using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Orders.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Tests;

/// <summary>One guest's share paid with two methods: "10 in cash, the remaining 2 to the card" (and the other way round).</summary>
public class MixedPaymentTests
{
    private readonly IOrderPaymentRepository _payments = Substitute.For<IOrderPaymentRepository>();
    private readonly ICounterpartyRepository _counterparties = Substitute.For<ICounterpartyRepository>();
    private readonly List<OrderPayment> _saved = new();
    private readonly OrderPaymentService _service;

    public MixedPaymentTests()
    {
        _payments.GetByOrderIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new List<OrderPayment>());
        _payments.AddAsync(Arg.Do<OrderPayment>(p => _saved.Add(p)), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(5);
        var settings = Substitute.For<ICompanySettingsRepository>();
        settings.GetByCompanyIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new CompanySettings { KitchenApprovalRequired = false });
        _counterparties.GetByIdAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => new Counterparty { Id = ci.ArgAt<int>(0), CompanyId = 1, IsBank = true, IsActive = true, Name = "Bank" });

        _service = new OrderPaymentService(
            Substitute.For<IOrderRepository>(), _payments, Substitute.For<IRecipeStockDeductionService>(),
            user, _counterparties, settings, NullLogger<OrderPaymentService>.Instance);
    }

    // A 30 ₼ bill: one line of 30.
    private static Order Bill()
    {
        var item = new MenuItem { Id = 1, Name = "Kabab", Price = 30m, UnitId = (int)UnitOfMeasure.Piece };
        var order = new Order { Id = 7, CompanyId = 1, Status = OrderStatus.Open, TotalAmount = 30m };
        order.Lines.Add(new OrderLine
        {
            Id = 1, OrderId = 7, MenuItemId = 1, MenuItem = item, Quantity = 1, UnitPrice = 30m, LineTotal = 30m, Status = OrderLineStatus.Pending,
        });
        return order;
    }

    [Fact]
    public async Task Share_of_12_pays_10_in_cash_and_the_remaining_2_goes_to_the_card()
    {
        var order = Bill();

        // The guest pays for an amount of 12 (not items): 10 handed over in cash, the rest to the card.
        var result = await _service.PayAsync(
            order, new List<PartPaymentLine>(), 12m, PaymentMethod.Cash, 10m, false, null, default,
            remainderMethod: PaymentMethod.Card, remainderBankId: 3);

        Assert.Equal(2, _saved.Count);
        var cash = _saved.Single(p => p.Method == PaymentMethod.Cash);
        var card = _saved.Single(p => p.Method == PaymentMethod.Card);
        Assert.Equal(10m, cash.Amount);
        Assert.Equal(0m, cash.ChangeAmount);
        Assert.Equal(2m, card.Amount);
        Assert.Equal(3, card.BankCounterpartyId);
        Assert.False(result.OrderClosed);
        Assert.Equal(18m, result.RemainingAmount);
    }

    [Fact]
    public async Task Whole_bill_of_30_pays_10_in_cash_and_the_rest_to_the_card_and_closes_the_order()
    {
        var order = Bill();

        var result = await _service.PayAsync(
            order, new List<PartPaymentLine>(), 30m, PaymentMethod.Cash, 10m, false, null, default,
            remainderMethod: PaymentMethod.Card, remainderBankId: 3);

        Assert.True(result.OrderClosed);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(10m, _saved.Single(p => p.Method == PaymentMethod.Cash).Amount);
        Assert.Equal(20m, _saved.Single(p => p.Method == PaymentMethod.Card).Amount);
        Assert.Equal(30m, _saved.Sum(p => p.Amount));
    }

    [Fact]
    public async Task Share_of_12_pays_10_on_the_card_and_the_remaining_2_goes_to_cash()
    {
        var order = Bill();

        await _service.PayAsync(
            order, new List<PartPaymentLine>(), 12m, PaymentMethod.Card, 10m, false, 3, default,
            remainderMethod: PaymentMethod.Cash);

        var card = _saved.Single(p => p.Method == PaymentMethod.Card);
        var cash = _saved.Single(p => p.Method == PaymentMethod.Cash);
        Assert.Equal(10m, card.Amount);
        Assert.Equal(10m, card.ReceivedAmount);
        Assert.Equal(2m, cash.Amount);
        Assert.Null(cash.BankCounterpartyId);
    }

    [Fact]
    public async Task Cash_that_covers_the_share_stays_a_single_payment_with_change()
    {
        var order = Bill();

        await _service.PayAsync(
            order, new List<PartPaymentLine>(), 12m, PaymentMethod.Cash, 20m, false, null, default,
            remainderMethod: PaymentMethod.Card, remainderBankId: 3);

        var only = Assert.Single(_saved);
        Assert.Equal(12m, only.Amount);
        Assert.Equal(8m, only.ChangeAmount);
    }

    [Fact]
    public async Task The_remainder_must_use_a_different_method()
    {
        var order = Bill();

        await Assert.ThrowsAsync<BadRequestException>(() => _service.PayAsync(
            order, new List<PartPaymentLine>(), 12m, PaymentMethod.Cash, 10m, false, null, default,
            remainderMethod: PaymentMethod.Cash));
    }

    [Fact]
    public async Task A_card_remainder_needs_its_bank()
    {
        var order = Bill();

        await Assert.ThrowsAsync<BadRequestException>(() => _service.PayAsync(
            order, new List<PartPaymentLine>(), 12m, PaymentMethod.Cash, 10m, false, null, default,
            remainderMethod: PaymentMethod.Card, remainderBankId: null));
    }

    [Fact]
    public async Task Without_a_remainder_method_cash_below_the_share_is_still_refused()
    {
        var order = Bill();

        await Assert.ThrowsAsync<BadRequestException>(() => _service.PayAsync(
            order, new List<PartPaymentLine>(), 12m, PaymentMethod.Cash, 10m, false, null, default));
    }
}
