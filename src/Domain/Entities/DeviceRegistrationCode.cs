using Domain.Common;

namespace Domain.Entities;

/// <summary>One-time code the SuperAdmin hands to a restaurant to register one of its devices.</summary>
public class DeviceRegistrationCode : CompanyEntity<int>
{
    /// <summary>SHA-256 of the code — the plain code is only shown once to the SuperAdmin.</summary>
    public string CodeHash { get; set; } = default!;

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public int? UsedByDeviceId { get; set; }
}
