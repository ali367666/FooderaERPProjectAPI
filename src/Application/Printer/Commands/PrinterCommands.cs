using Application.Printer.Dtos;
using MediatR;

namespace Application.Printer.Commands;

public record CreatePrinterCommand(CreatePrinterRequest Request) : IRequest<PrinterResponse>;

public record UpdatePrinterCommand(UpdatePrinterRequest Request) : IRequest<PrinterResponse>;

public record DeletePrinterCommand(int Id) : IRequest;

public record PrintToPrinterCommand(int PrinterId, string Content) : IRequest;
