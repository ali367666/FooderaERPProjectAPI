using Application.Common.Interfaces.Abstracts.İnterfaces;
using MediatR;

namespace Application.Printer.Queries;

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
