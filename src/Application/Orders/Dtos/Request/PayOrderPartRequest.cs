namespace Application.Orders.Dtos.Request;

/// <summary>One guest's share of the bill: which items (and how many of each) they pay for, and how.</summary>
public class PayOrderPartRequest
{
    public string PaymentMethod { get; set; } = default!;

    /// <summary>Money handed over. Ignored for credit ("borca yaz").</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Pay one guest's share with two methods: <see cref="PaymentMethod"/> takes <see cref="PaidAmount"/> (cash handed over, or the sum
    /// put on the card) and, when that is less than the share, the rest goes automatically to this method (Cash or Card, the other one).
    /// E.g. a share of 12: 10 in cash and the remaining 2 to the card. Empty = a single method as before.
    /// </summary>
    public string? RemainderPaymentMethod { get; set; }

    /// <summary>The bank the remainder goes through when <see cref="RemainderPaymentMethod"/> is Card.</summary>
    public int? RemainderBankId { get; set; }

    /// <summary>The bank a card payment goes through (a counterparty with IsBank). Required for Card.</summary>
    public int? BankId { get; set; }

    /// <summary>Rung through the fiscal (tax) register.</summary>
    public bool IsFiscal { get; set; }

    /// <summary>
    /// Pay "by amount": settle this sum of the bill with no items attached (e.g. 30 in cash, the
    /// rest by card). Capped at what is left; <see cref="Lines"/> is then empty.
    /// </summary>
    public decimal? Amount { get; set; }

    public List<PayOrderPartLine> Lines { get; set; } = new();
}
