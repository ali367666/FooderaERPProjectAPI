namespace Application.Common.Interfaces;

/// <summary>
/// Online/Offline gate: with an active Online licence a company's users may work from anywhere;
/// otherwise only from the company's registered devices.
/// </summary>
public enum AccessDecision
{
    Allowed,
    /// <summary>Cloud, no active Online licence and this isn't a registered device.</summary>
    DeviceNotRegistered,
    /// <summary>Local installation without a valid licence key.</summary>
    LicenseRequired
}
