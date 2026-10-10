using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.ScaleDevice.Commands;
using Application.ScaleDevice.Dtos;
using MediatR;

namespace Application.ScaleDevice.Queries;

public record GetAllScaleDevicesQuery(int RestaurantId) : IRequest<List<ScaleDeviceResponse>>;
