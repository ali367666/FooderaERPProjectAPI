using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Money the company put in. The first entry is the opening deposit - the amount the company starts working
/// with; later entries are additional deposits. When (CreatedAtUtc) and by whom (CreatedByUserId) are kept.
/// </summary>
public class CompanyDeposit : CompanyEntity<int>
{
    /// <summary>Amount in AZN.</summary>
    public decimal Amount { get; set; }

    /// <summary>True for the amount the company started with (one per company).</summary>
    public bool IsOpening { get; set; }

    public string? Note { get; set; }
}
