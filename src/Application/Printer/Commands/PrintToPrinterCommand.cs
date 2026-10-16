using Application.Printer.Dtos;
using MediatR;

namespace Application.Printer.Commands;

public record PrintToPrinterCommand(int PrinterId, string Content) : IRequest;
