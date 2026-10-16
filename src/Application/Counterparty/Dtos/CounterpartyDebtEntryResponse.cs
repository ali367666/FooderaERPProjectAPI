namespace Application.Counterparty.Dtos;

public class CounterpartyDebtEntryResponse
{
    public int Id { get; set; }

    /// <summary>Added, Adjusted or CreditSale.</summary>
    public string Type { get; set; } = default!;

    /// <summary>Positive raised the debt, negative lowered it.</summary>
    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }
    public string? Note { get; set; }
    public int? OrderId { get; set; }
    public string? OrderNumber { get; set; }

    /// <summary>When the change was made.</summary>
    public DateTime CreatedAtUtc { get; set; }
}
