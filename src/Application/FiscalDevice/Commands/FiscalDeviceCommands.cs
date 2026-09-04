using Application.FiscalDevice.Dtos;
using MediatR;

namespace Application.FiscalDevice.Commands;

public record CreateFiscalDeviceCommand(CreateFiscalDeviceRequest Request) : IRequest<FiscalDeviceResponse>;

public record UpdateFiscalDeviceCommand(UpdateFiscalDeviceRequest Request) : IRequest<FiscalDeviceResponse>;

public record DeleteFiscalDeviceCommand(int Id) : IRequest;
