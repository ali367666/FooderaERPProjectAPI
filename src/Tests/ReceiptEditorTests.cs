using Application.Orders.Services;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Tests;

/// <summary>Correcting a paid receipt: another payment method, or another quantity of one line.</summary>
public class ReceiptEditorTests
{
    private static MenuItem Item(string name) => new() { Id = name.GetHashCode() & 0xFFFF, Name = name, UnitId = (int)UnitOfMeasure.Piece };

    // A receipt of 2 colas (2 x 3 = 6) and 1 kebab (12): bill 18, paid in cash with 20 handed over.
    private static (Order Order, OrderLine Cola, OrderLine Kebab, OrderPayment Payment) PaidCashReceipt()
    {
        var order = new Order { Id = 1, CompanyId = 1, Status = OrderStatus.Paid, IsPaid = true, PaymentMethod = PaymentMethod.Cash, TotalAmount = 18m, PaidAmount = 20m, ChangeAmount = 2m };
        var cola = new OrderLine { Id = 1, OrderId = 1, MenuItem = Item("Cola"), Quantity = 2, UnitPrice = 3m, LineTotal = 6m, Status = OrderLineStatus.Served };
        var kebab = new OrderLine { Id = 2, OrderId = 1, MenuItem = Item("Kabab"), Quantity = 1, UnitPrice = 12m, LineTotal = 12m, Status = OrderLineStatus.Served };
        order.Lines.Add(cola);
        order.Lines.Add(kebab);
        var payment = new OrderPayment { Id = 1, OrderId = 1, Method = PaymentMethod.Cash, Amount = 18m, ReceivedAmount = 20m, ChangeAmount = 2m };
        payment.Lines.Add(new OrderPaymentLine { OrderLineId = 1, Quantity = 2, Amount = 6m });
        payment.Lines.Add(new OrderPaymentLine { OrderLineId = 2, Quantity = 1, Amount = 12m });
        return (order, cola, kebab, payment);
    }

    [Fact]
    public void Cash_changed_to_card_pays_exactly_what_is_due_with_no_change()
    {
        var (order, _, _, payment) = PaidCashReceipt();

        ReceiptEditor.ApplyPaymentMethod(order, new[] { payment }, payment, PaymentMethod.Card, new Counterparty { Id = 5 });

        Assert.Equal(PaymentMethod.Card, payment.Method);
        Assert.Equal(5, payment.BankCounterpartyId);
        Assert.Equal(18m, payment.ReceivedAmount);
        Assert.Equal(0m, payment.ChangeAmount);
        Assert.Equal(PaymentMethod.Card, order.PaymentMethod);
        Assert.Equal(5, order.BankCounterpartyId);
        Assert.Equal(0m, order.ChangeAmount);
    }

    [Fact]
    public void Card_changed_to_cash_drops_the_bank()
    {
        var (order, _, _, payment) = PaidCashReceipt();
        ReceiptEditor.ApplyPaymentMethod(order, new[] { payment }, payment, PaymentMethod.Card, new Counterparty { Id = 5 });

        ReceiptEditor.ApplyPaymentMethod(order, new[] { payment }, payment, PaymentMethod.Cash, null);

        Assert.Equal(PaymentMethod.Cash, payment.Method);
        Assert.Null(payment.BankCounterpartyId);
        Assert.Null(order.BankCounterpartyId);
    }

    [Fact]
    public void A_receipt_without_payment_rows_changes_its_own_method()
    {
        var (order, _, _, _) = PaidCashReceipt();

        ReceiptEditor.ApplyPaymentMethod(order, Array.Empty<OrderPayment>(), null, PaymentMethod.Card, new Counterparty { Id = 3 });

        Assert.Equal(PaymentMethod.Card, order.PaymentMethod);
        Assert.Equal(18m, order.PaidAmount);
        Assert.Equal(0m, order.ChangeAmount);
    }

    [Fact]
    public void One_cola_less_lowers_the_total_the_payment_and_its_lines()
    {
        var (order, cola, _, payment) = PaidCashReceipt();

        ReceiptEditor.ApplyLineQuantity(order, cola, 1, new[] { payment });

        Assert.Equal(15m, order.TotalAmount);
        Assert.Equal(15m, payment.Amount);
        Assert.Equal(5m, payment.ChangeAmount); // 20 handed over, 15 due
        Assert.Equal(15m, payment.Lines.Sum(l => l.Amount));
        Assert.Equal(1, payment.Lines.First(l => l.OrderLineId == 1).Quantity);
    }

    [Fact]
    public void Removing_a_line_cancels_it_and_drops_its_payment_line()
    {
        var (order, cola, _, payment) = PaidCashReceipt();

        ReceiptEditor.ApplyLineQuantity(order, cola, 0, new[] { payment });

        Assert.Equal(OrderLineStatus.Cancelled, cola.Status);
        Assert.Equal(12m, order.TotalAmount);
        Assert.Single(payment.Lines);
    }

    [Fact]
    public void More_than_was_handed_over_raises_the_cash_received_to_the_new_total()
    {
        var (order, cola, _, payment) = PaidCashReceipt();

        ReceiptEditor.ApplyLineQuantity(order, cola, 10, new[] { payment }); // 30 + 12 = 42

        Assert.Equal(42m, order.TotalAmount);
        Assert.Equal(42m, payment.ReceivedAmount);
        Assert.Equal(0m, payment.ChangeAmount);
    }

    [Fact]
    public void A_fixed_discount_never_exceeds_what_is_left_to_discount()
    {
        var (order, cola, kebab, payment) = PaidCashReceipt();
        order.DiscountAmount = 10m;
        order.TotalAmount = 8m;
        payment.Amount = 8m;

        ReceiptEditor.ApplyLineQuantity(order, kebab, 0, new[] { payment }); // only 6 of colas left

        Assert.Equal(6m, order.DiscountAmount);
        Assert.Equal(0m, order.TotalAmount);
    }

    [Fact]
    public void The_service_charge_stays_on_the_total()
    {
        var (order, cola, _, payment) = PaidCashReceipt();
        order.ServiceChargeAmount = 1.8m;
        order.TotalAmount = 19.8m;
        payment.ServiceChargeAmount = 1.8m;

        ReceiptEditor.ApplyLineQuantity(order, cola, 1, new[] { payment });

        Assert.Equal(16.8m, order.TotalAmount);
        Assert.Equal(15m, payment.Amount);
    }
}
