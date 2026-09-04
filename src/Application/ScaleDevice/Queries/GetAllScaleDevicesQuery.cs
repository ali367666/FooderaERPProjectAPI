using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.ScaleDevice.Commands;
using Application.ScaleDevice.Dtos;
using MediatR;

namespace Application.ScaleDevice.Queries;

public record GetAllScaleDevicesQuery(int RestaurantId) : IRequest<List<ScaleDeviceResponse>>;

public class GetAllScaleDevicesQueryHandler : IRequestHandler<GetAllScaleDevicesQuery, List<ScaleDeviceResponse>>
{
    private readonly IScaleDeviceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllScaleDevicesQueryHandler(IScaleDeviceRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<ScaleDeviceResponse>> Handle(GetAllScaleDevicesQuery request, CancellationToken cancellationToken)
    {
        var devices = await _repository.GetAllByRestaurantAsync(_currentUserService.CompanyId, request.RestaurantId, cancellationToken);
        return devices.Select(CreateScaleDeviceCommandHandler.Map).ToList();
    }
}
