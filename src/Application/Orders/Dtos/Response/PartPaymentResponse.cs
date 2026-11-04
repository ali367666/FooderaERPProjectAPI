namespace Application.Orders.Dtos;

public class PartPaymentResponse
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public decimal ChangeAmount { get; set; }
    public bool OrderClosed { get; set; }
    public decimal RemainingAmount { get; set; }

    /// <summary>What was taken with each method for this share (the service charge included): one entry, or two when the share was split.</summary>
    public List<PartPaymentPart> Parts { get; set; } = new();
}

public class PartPaymentPart
{
    public string Method { get; set; } = default!;
    public decimal Amount { get; set; }
}
