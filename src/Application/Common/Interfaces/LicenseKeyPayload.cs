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
