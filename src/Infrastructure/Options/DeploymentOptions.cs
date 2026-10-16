namespace Infrastructure.Options;

/// <summary>appsettings "Deployment".</summary>
public class DeploymentOptions
{
    /// <summary>"Cloud" (default) or "Local".</summary>
    public string Mode { get; set; } = "Cloud";
}
