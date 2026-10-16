using Application.Printer.Dtos;
using MediatR;

namespace Application.Printer.Commands;

public record CreatePrinterCommand(CreatePrinterRequest Request) : IRequest<PrinterResponse>;
