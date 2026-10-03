namespace Application.Common.Interfaces;

/// <summary>
/// What a signed licence key grants. Local installations verify it offline with the platform's
/// public key, so they keep working without internet until <see cref="ExpiresAtUtc"/>.
/// </summary>
public sealed record LicenseKeyPayload(
    string CompanyCode,
    string CompanyName,
    string DeploymentType,
    bool RemoteAccess,
    bool OfflineMode,
    DateTime ExpiresAtUtc,
    DateTime IssuedAtUtc);

public interface ILicenseKeyService
{
    /// <summary>Central server only (holds the private key).</summary>
    bool CanIssue { get; }

    string Issue(LicenseKeyPayload payload);

    /// <summary>Null when the key is malformed or its signature doesn't match.</summary>
    LicenseKeyPayload? Verify(string key);
}

/// <summary>Where this instance of the system runs (appsettings "Deployment:Mode").</summary>
public interface IDeploymentInfo
{
    /// <summary>Installed on a restaurant's own computer — licensed by key, no device gate.</summary>
    bool IsLocal { get; }
}
