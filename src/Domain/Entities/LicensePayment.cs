using Domain.Common;

namespace Domain.Entities;

/// <summary>A manually recorded licence payment ("Ödənildi, 1 ay uzat").</summary>
public class LicensePayment : CompanyEntity<int>
{
    public int Months { get; set; }
    public decimal Amount { get; set; }
    public DateTime PeriodFromUtc { get; set; }
    public DateTime PeriodToUtc { get; set; }
    public string? Note { get; set; }

    public User? CreatedByUser { get; set; }
}
