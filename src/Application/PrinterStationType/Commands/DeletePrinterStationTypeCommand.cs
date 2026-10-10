using Application.PrinterStationType.Dtos;
using MediatR;

namespace Application.PrinterStationType.Commands;

public record DeletePrinterStationTypeCommand(int Id) : IRequest;
