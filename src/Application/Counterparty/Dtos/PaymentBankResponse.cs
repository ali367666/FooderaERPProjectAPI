namespace Application.Counterparty.Dtos;

/// <summary>A payment bank the POS offers on a card payment (id and name only).</summary>
public class PaymentBankResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}
