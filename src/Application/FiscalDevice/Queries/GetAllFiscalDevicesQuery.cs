using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.FiscalDevice.Commands;
using Application.FiscalDevice.Dtos;
using MediatR;

namespace Application.FiscalDevice.Queries;

public record GetAllFiscalDevicesQuery(int RestaurantId) : IRequest<List<FiscalDeviceResponse>>;

public class GetAllFiscalDevicesQueryHandler : IRequestHandler<GetAllFiscalDevicesQuery, List<FiscalDeviceResponse>>
{
    private readonly IFiscalDeviceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllFiscalDevicesQueryHandler(IFiscalDeviceRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<FiscalDeviceResponse>> Handle(GetAllFiscalDevicesQuery request, CancellationToken cancellationToken)
    {
        var devices = await _repository.GetAllByRestaurantAsync(_currentUserService.CompanyId, request.RestaurantId, cancellationToken);
        return devices.Select(CreateFiscalDeviceCommandHandler.Map).ToList();
    }
}
