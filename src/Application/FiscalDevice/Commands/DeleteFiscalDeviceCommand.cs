using Application.FiscalDevice.Dtos;
using MediatR;

namespace Application.FiscalDevice.Commands;

public record DeleteFiscalDeviceCommand(int Id) : IRequest;
