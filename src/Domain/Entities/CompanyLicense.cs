using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Paid features of a company, managed only by the platform SuperAdmin.
/// "Online" (remote access) lets the company's users work from any device; without it the system
/// can only be used from the company's registered devices (the restaurant's own tills/terminals).
/// </summary>
public class CompanyLicense : CompanyEntity<int>
{
    public DeploymentType DeploymentType { get; set; } = DeploymentType.Cloud;

    /// <summary>Online / remote access switched on by the SuperAdmin.</summary>
    public bool RemoteAccessEnabled { get; set; }

    /// <summary>Paid until — remote access stops on its own after this moment.</summary>
    public DateTime? RemoteAccessExpiresAtUtc { get; set; }

    /// <summary>Keep selling while the internet is down (separate feature, phase 2).</summary>
    public bool OfflineModeEnabled { get; set; }

    public decimal? MonthlyPrice { get; set; }

    /// <summary>Last expiry reminder sent (3, 1 or 0 days left) — reset whenever the licence is extended.</summary>
    public int? LastExpiryNoticeDays { get; set; }

    /// <summary>Signed licence key — issued centrally for a Local installation, imported there.</summary>
    public string? LicenseKey { get; set; }

    /// <summary>Until when the licence key lets a Local installation run.</summary>
    public DateTime? LicenseKeyExpiresAtUtc { get; set; }

    public bool IsRemoteAccessActive(DateTime utcNow) =>
        RemoteAccessEnabled && RemoteAccessExpiresAtUtc is { } until && until > utcNow;

    public bool IsLicenseKeyValid(DateTime utcNow) =>
        LicenseKeyExpiresAtUtc is { } until && until > utcNow;

    /// <summary>The date reminders count down to: the key for Local installs, Online otherwise.</summary>
    public DateTime? ReminderExpiresAtUtc =>
        DeploymentType == DeploymentType.Local ? LicenseKeyExpiresAtUtc
        : RemoteAccessEnabled ? RemoteAccessExpiresAtUtc
        : null;
}
