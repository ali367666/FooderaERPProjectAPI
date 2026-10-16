using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.PrinterStationType.Commands;
using Application.PrinterStationType.Dtos;
using MediatR;

namespace Application.PrinterStationType.Queries;

public record GetAllPrinterStationTypesQuery : IRequest<List<PrinterStationTypeResponse>>;
