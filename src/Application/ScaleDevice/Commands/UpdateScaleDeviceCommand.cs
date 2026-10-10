using Application.ScaleDevice.Dtos;
using MediatR;

namespace Application.ScaleDevice.Commands;

public record UpdateScaleDeviceCommand(UpdateScaleDeviceRequest Request) : IRequest<ScaleDeviceResponse>;
