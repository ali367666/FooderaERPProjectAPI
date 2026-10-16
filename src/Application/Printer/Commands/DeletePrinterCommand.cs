using Application.Printer.Dtos;
using MediatR;

namespace Application.Printer.Commands;

public record DeletePrinterCommand(int Id) : IRequest;
