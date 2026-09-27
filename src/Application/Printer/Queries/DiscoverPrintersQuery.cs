using Application.Common.Interfaces.Abstracts.İnterfaces;
using MediatR;

namespace Application.Printer.Queries;

/// <summary>"Lupa" on the printer form — printers installed on this computer and found on the local network.</summary>
public record DiscoverPrintersQuery : IRequest<List<DiscoveredPrinter>>;

public class DiscoverPrintersQueryHandler : IRequestHandler<DiscoverPrintersQuery, List<DiscoveredPrinter>>
{
    private readonly IPrinterDiscoveryService _discoveryService;

    public DiscoverPrintersQueryHandler(IPrinterDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    public Task<List<DiscoveredPrinter>> Handle(DiscoverPrintersQuery request, CancellationToken cancellationToken) =>
        _discoveryService.DiscoverAsync(cancellationToken);
}
