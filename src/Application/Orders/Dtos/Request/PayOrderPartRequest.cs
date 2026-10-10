namespace Application.Orders.Dtos.Request;

/// <summary>One guest's share of the bill: which items (and how many of each) they pay for, and how.</summary>
public class PayOrderPartRequest
{
    public string PaymentMethod { get; set; } = default!;

    /// <summary>Money handed over. Ignored for credit ("borca yaz").</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>Rung through the fiscal (tax) register.</summary>
    public bool IsFiscal { get; set; }

    /// <summary>
    /// Pay "by amount": settle this sum of the bill with no items attached (e.g. 30 in cash, the
    /// rest by card). Capped at what is left; <see cref="Lines"/> is then empty.
    /// </summary>
    public decimal? Amount { get; set; }

    public List<PayOrderPartLine> Lines { get; set; } = new();
}
