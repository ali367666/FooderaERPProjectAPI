namespace Application.Common.Interfaces.Abstracts.İnterfaces;

public interface IPrinterDiscoveryService
{
    /// <summary>
    /// Printers installed on the machine running the API (Windows) plus raw-TCP (port 9100)
    /// printers answering on the local network.
    /// </summary>
    Task<List<DiscoveredPrinter>> DiscoverAsync(CancellationToken cancellationToken);
}

public class DiscoveredPrinter
{
    public string Name { get; set; } = default!;
    /// <summary>Null for a locally attached (USB) printer — it can't be reached over the network.</summary>
    public string? IpAddress { get; set; }
    public int Port { get; set; } = 9100;
    /// <summary>"installed" (printer installed on this computer) or "network" (found on the LAN).</summary>
    public string Source { get; set; } = default!;
}
