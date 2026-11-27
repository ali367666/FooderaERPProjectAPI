using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A manually recorded company income or expense (rent, salary, utilities, other income...).
/// Sales, stock purchases and till withdrawals are not stored here — the finance report reads those
/// straight from their own tables.
/// </summary>
public class FinanceEntry : CompanyEntity<int>
{
    /// <summary>Null = company-wide, otherwise tied to one branch.</summary>
    public int? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public FinanceEntryType Type { get; set; }

    /// <summary>What it is, e.g. "İcarə", "Elektrik", "Maaş". Same names are totalled together in the report.</summary>
    public string Name { get; set; } = default!;

    public decimal Amount { get; set; }

    /// <summary>Business date the entry belongs to (time part ignored).</summary>
    public DateTime EntryDate { get; set; }

    public string? Note { get; set; }

    public User? CreatedByUser { get; set; }
}
