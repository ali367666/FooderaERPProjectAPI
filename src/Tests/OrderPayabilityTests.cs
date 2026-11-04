using Application.Orders.Services;
using Domain.Enums;
using Xunit;

namespace Tests;

public class OrderPayabilityTests
{
    [Theory]
    [InlineData(OrderStatus.Ready, true)]
    [InlineData(OrderStatus.Served, true)]
    [InlineData(OrderStatus.Open, false)]
    [InlineData(OrderStatus.InPreparation, false)]
    [InlineData(OrderStatus.Draft, false)]
    public void With_kitchen_approval_only_ready_or_served_orders_can_be_paid(OrderStatus status, bool expected)
        => Assert.Equal(expected, OrderPayability.CanPay(status, kitchenApprovalRequired: true));

    [Theory]
    [InlineData(OrderStatus.Open, true)]
    [InlineData(OrderStatus.InPreparation, true)]
    [InlineData(OrderStatus.Ready, true)]
    [InlineData(OrderStatus.Served, true)]
    [InlineData(OrderStatus.Draft, false)]
    [InlineData(OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Cancelled, false)]
    public void Without_kitchen_approval_any_order_sent_to_the_kitchen_can_be_paid(OrderStatus status, bool expected)
        => Assert.Equal(expected, OrderPayability.CanPay(status, kitchenApprovalRequired: false));
}
