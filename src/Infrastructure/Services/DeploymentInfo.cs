using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class DeploymentInfo : IDeploymentInfo
{
    public DeploymentInfo(IOptions<DeploymentOptions> options)
    {
        IsLocal = string.Equals(options.Value.Mode, "Local", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsLocal { get; }
}
