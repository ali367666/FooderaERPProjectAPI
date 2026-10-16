using Application.ScaleDevice.Dtos;
using MediatR;

namespace Application.ScaleDevice.Commands;

public record DeleteScaleDeviceCommand(int Id) : IRequest;
