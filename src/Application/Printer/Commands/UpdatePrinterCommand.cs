using Application.Printer.Dtos;
using MediatR;

namespace Application.Printer.Commands;

public record UpdatePrinterCommand(UpdatePrinterRequest Request) : IRequest<PrinterResponse>;
