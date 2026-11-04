namespace Application.Orders.Dtos.Request;

public class PayOrderRequest
{
    public string PaymentMethod { get; set; } = default!;
    public decimal PaidAmount { get; set; }

    /// <summary>Settling what is left of a bill that already has part payments with two methods; see <see cref="PayOrderPartRequest.RemainderPaymentMethod"/>.</summary>
    public string? RemainderPaymentMethod { get; set; }
    public int? RemainderBankId { get; set; }

    /// <summary>The bank a card payment goes through (a counterparty with IsBank). Required for Card.</summary>
    public int? BankId { get; set; }

    /// <summary>Rung through the fiscal (tax) register.</summary>
    public bool IsFiscal { get; set; }
}
