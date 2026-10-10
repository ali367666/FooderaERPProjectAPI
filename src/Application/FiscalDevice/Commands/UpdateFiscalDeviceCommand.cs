using Application.FiscalDevice.Dtos;
using MediatR;

namespace Application.FiscalDevice.Commands;

public record UpdateFiscalDeviceCommand(UpdateFiscalDeviceRequest Request) : IRequest<FiscalDeviceResponse>;
