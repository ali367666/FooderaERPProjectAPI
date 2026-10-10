using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

// ---------------------------------------------------------------- DTOs

public class LicenseStatusResponse
{
    public int CompanyId { get; set; }
    public string DeploymentType { get; set; } = default!;
    public bool RemoteAccessEnabled { get; set; }
    /// <summary>Online right now (enabled and not expired).</summary>
    public bool RemoteAccessActive { get; set; }
    public DateTime? RemoteAccessExpiresAtUtc { get; set; }
    public int? DaysLeft { get; set; }
    public bool OfflineModeEnabled { get; set; }
    /// <summary>The calling device is one of the company's registered devices.</summary>
    public bool DeviceRegistered { get; set; }

    /// <summary>This server is a Local installation (licensed by key).</summary>
    public bool IsLocalInstallation { get; set; }
    public DateTime? LicenseKeyExpiresAtUtc { get; set; }
    public bool LicenseKeyValid { get; set; }
}
