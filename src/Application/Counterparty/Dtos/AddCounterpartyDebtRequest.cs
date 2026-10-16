namespace Application.Counterparty.Dtos;

public class AddCounterpartyDebtRequest
{
    /// <summary>The amount of debt to add on top of what the counterparty already owes.</summary>
    public decimal Amount { get; set; }

    public string? Note { get; set; }
}
