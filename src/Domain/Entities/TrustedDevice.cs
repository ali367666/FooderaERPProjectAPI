using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// A till/terminal registered to a company with a one-time code from the SuperAdmin. Without the
/// Online licence the company's users can only work from these devices.
/// </summary>
public class TrustedDevice : CompanyEntity<int>
{
    public string Name { get; set; } = default!;

    /// <summary>SHA-256 of the secret key the device keeps and sends with every request.</summary>
    public string KeyHash { get; set; } = default!;

    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenAtUtc { get; set; }
}
