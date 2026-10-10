using Application.FiscalDevice.Dtos;
using MediatR;

namespace Application.FiscalDevice.Commands;

public record CreateFiscalDeviceCommand(CreateFiscalDeviceRequest Request) : IRequest<FiscalDeviceResponse>;
