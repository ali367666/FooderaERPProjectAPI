using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.FiscalDevice.Commands;
using Application.FiscalDevice.Dtos;
using MediatR;

namespace Application.FiscalDevice.Queries;

public record GetAllFiscalDevicesQuery(int RestaurantId) : IRequest<List<FiscalDeviceResponse>>;
