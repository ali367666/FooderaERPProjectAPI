using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.ScaleDevice.Dtos;
using MediatR;

namespace Application.ScaleDevice.Commands;

public class UpdateScaleDeviceCommandHandler : IRequestHandler<UpdateScaleDeviceCommand, ScaleDeviceResponse>
{
    private readonly IScaleDeviceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateScaleDeviceCommandHandler(IScaleDeviceRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<ScaleDeviceResponse> Handle(UpdateScaleDeviceCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var device = await _repository.GetByIdAsync(dto.Id, companyId, cancellationToken);
        if (device is null)
            throw new Exception("Tərəzi tapılmadı.");

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(device.RestaurantId, name, device.Id, cancellationToken))
            throw new Exception("Bu adda tərəzi artıq mövcuddur.");

        device.Name = name;
        device.Brand = string.IsNullOrWhiteSpace(dto.Brand) ? null : dto.Brand.Trim();
        device.ConnectionInfo = string.IsNullOrWhiteSpace(dto.ConnectionInfo) ? null : dto.ConnectionInfo.Trim();
        device.IsActive = dto.IsActive;

        _repository.Update(device);
        await _repository.SaveChangesAsync(cancellationToken);

        return CreateScaleDeviceCommandHandler.Map(device);
    }
}
