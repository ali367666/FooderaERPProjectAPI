using Application.PrinterStationType.Dtos;
using MediatR;

namespace Application.PrinterStationType.Commands;

public record CreatePrinterStationTypeCommand(CreatePrinterStationTypeRequest Request) : IRequest<PrinterStationTypeResponse>;
