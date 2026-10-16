namespace Application.Common.Interfaces.Abstracts.İnterfaces;

public interface IPrinterDiscoveryService
{
    /// <summary>
    /// Printers installed on the machine running the API (Windows) plus raw-TCP (port 9100)
    /// printers answering on the local network.
    /// </summary>
    Task<List<DiscoveredPrinter>> DiscoverAsync(CancellationToken cancellationToken);
}
