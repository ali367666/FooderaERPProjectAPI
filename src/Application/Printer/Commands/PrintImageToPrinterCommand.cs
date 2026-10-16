using Application.Printer.Dtos;
using MediatR;

namespace Application.Printer.Commands;

/// <param name="Data">Base64 of the packed 1-bit rows (MSB first, 1 = black), WidthDots/8 bytes per row.</param>
public record PrintImageToPrinterCommand(int PrinterId, int WidthDots, int Height, string Data, string? Trailer) : IRequest;
