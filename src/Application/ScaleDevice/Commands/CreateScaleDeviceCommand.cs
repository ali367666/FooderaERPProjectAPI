using Application.ScaleDevice.Dtos;
using MediatR;

namespace Application.ScaleDevice.Commands;

public record CreateScaleDeviceCommand(CreateScaleDeviceRequest Request) : IRequest<ScaleDeviceResponse>;
