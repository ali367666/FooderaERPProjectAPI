namespace Infrastructure.Options;

/// <summary>appsettings "Licensing" — ECDSA P-256 keys in PEM format.</summary>
public class LicensingOptions
{
    /// <summary>Central server only. Never ship this to a local installation.</summary>
    public string? PrivateKeyPem { get; set; }

    /// <summary>Every installation — verifies licence keys offline.</summary>
    public string? PublicKeyPem { get; set; }
}

/// <summary>appsettings "Deployment".</summary>
public class DeploymentOptions
{
    /// <summary>"Cloud" (default) or "Local".</summary>
    public string Mode { get; set; } = "Cloud";
}
