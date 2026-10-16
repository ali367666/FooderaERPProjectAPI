using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Printer.Commands;
using Application.Printer.Dtos;
using MediatR;

namespace Application.Printer.Queries;

public record GetAllPrintersQuery(int RestaurantId) : IRequest<List<PrinterResponse>>;
