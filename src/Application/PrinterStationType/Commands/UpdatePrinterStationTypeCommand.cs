using Application.PrinterStationType.Dtos;
using MediatR;

namespace Application.PrinterStationType.Commands;

public record UpdatePrinterStationTypeCommand(UpdatePrinterStationTypeRequest Request) : IRequest<PrinterStationTypeResponse>;
