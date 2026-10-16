namespace Application.Orders.Dtos;

public class OrderPaymentsResponse
{
    public List<OrderPaymentResponse> Payments { get; set; } = new();

    /// <summary>How much of each line is already paid for.</summary>
    public List<PaidLineResponse> PaidLines { get; set; } = new();

    /// <summary>Σ of the payments' amounts (without service charge).</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>What is still to pay of the bill — lines − discount + table rental, without service charge.</summary>
    public decimal RemainingAmount { get; set; }

    /// <summary>Share of each line's price left after the order discount (1 = no discount).</summary>
    public decimal DiscountFactor { get; set; } = 1m;
}
