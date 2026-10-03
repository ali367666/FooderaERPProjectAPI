namespace Domain.Enums;

/// <summary>Where a company's system runs — chosen per customer by the platform admin.</summary>
public enum DeploymentType
{
    /// <summary>On the central server; needs internet.</summary>
    Cloud = 1,

    /// <summary>Installed on the restaurant's own computer (no/poor internet locations).</summary>
    Local = 2
}
