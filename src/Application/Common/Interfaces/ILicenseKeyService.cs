namespace Application.Common.Interfaces;

public interface ILicenseKeyService
{
    /// <summary>Central server only (holds the private key).</summary>
    bool CanIssue { get; }

    string Issue(LicenseKeyPayload payload);

    /// <summary>Null when the key is malformed or its signature doesn't match.</summary>
    LicenseKeyPayload? Verify(string key);
}
