namespace Application.Common.Interfaces;

/// <summary>Where this instance of the system runs (appsettings "Deployment:Mode").</summary>
public interface IDeploymentInfo
{
    /// <summary>Installed on a restaurant's own computer — licensed by key, no device gate.</summary>
    bool IsLocal { get; }
}
