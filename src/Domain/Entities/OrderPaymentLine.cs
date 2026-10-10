namespace Domain.Entities;

/// <summary>The quantity of one order line a payment covers, and its share of the amount.</summary>
public class OrderPaymentLine
{
    public int Id { get; set; }

    public int OrderPaymentId { get; set; }
    public OrderPayment OrderPayment { get; set; } = default!;

    public int OrderLineId { get; set; }
    public OrderLine OrderLine { get; set; } = default!;

    /// <summary>Same unit as <see cref="OrderLine.Quantity"/> (grams for weight items).</summary>
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
}
