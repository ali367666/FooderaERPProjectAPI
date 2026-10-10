using Application.Common.Interfaces.Abstracts.İnterfaces;
using MediatR;

namespace Application.Printer.Queries;

/// <summary>"Lupa" on the printer form — printers installed on this computer and found on the local network.</summary>
public record DiscoverPrintersQuery : IRequest<List<DiscoveredPrinter>>;
