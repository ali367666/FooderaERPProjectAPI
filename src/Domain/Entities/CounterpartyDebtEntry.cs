using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// One change of a counterparty's debt. <see cref="BaseEntity{T}.CreatedAtUtc"/> is when it happened, so
/// the history shows the date and time of every amount that was added or paid off.
/// </summary>
public class CounterpartyDebtEntry : CompanyEntity<int>
{
    public int CounterpartyId { get; set; }
    public Counterparty Counterparty { get; set; } = default!;

    public CounterpartyDebtEntryType Type { get; set; }

    /// <summary>Positive raises the debt, negative lowers it.</summary>
    public decimal Amount { get; set; }

    /// <summary>The debt right after this change.</summary>
    public decimal BalanceAfter { get; set; }

    public string? Note { get; set; }

    /// <summary>The order that was put on credit (for <see cref="CounterpartyDebtEntryType.CreditSale"/>).</summary>
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
}
