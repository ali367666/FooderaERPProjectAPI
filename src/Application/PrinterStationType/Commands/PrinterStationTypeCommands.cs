using Application.PrinterStationType.Dtos;
using MediatR;

namespace Application.PrinterStationType.Commands;

public record CreatePrinterStationTypeCommand(CreatePrinterStationTypeRequest Request) : IRequest<PrinterStationTypeResponse>;

public record UpdatePrinterStationTypeCommand(UpdatePrinterStationTypeRequest Request) : IRequest<PrinterStationTypeResponse>;

public record DeletePrinterStationTypeCommand(int Id) : IRequest;
