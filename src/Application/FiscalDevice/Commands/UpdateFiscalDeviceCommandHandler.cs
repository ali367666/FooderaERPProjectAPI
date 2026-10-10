using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.FiscalDevice.Dtos;
using MediatR;

namespace Application.FiscalDevice.Commands;

public class UpdateFiscalDeviceCommandHandler : IRequestHandler<UpdateFiscalDeviceCommand, FiscalDeviceResponse>
{
    private readonly IFiscalDeviceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateFiscalDeviceCommandHandler(IFiscalDeviceRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<FiscalDeviceResponse> Handle(UpdateFiscalDeviceCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var device = await _repository.GetByIdAsync(dto.Id, companyId, cancellationToken);
        if (device is null)
            throw new Exception("Fiskal cihaz tapılmadı.");

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(device.RestaurantId, name, device.Id, cancellationToken))
            throw new Exception("Bu adda fiskal cihaz artıq mövcuddur.");

        device.Name = name;
        device.Provider = dto.Provider;
        device.ConnectionInfo = string.IsNullOrWhiteSpace(dto.ConnectionInfo) ? null : dto.ConnectionInfo.Trim();
        device.IsActive = dto.IsActive;

        _repository.Update(device);
        await _repository.SaveChangesAsync(cancellationToken);

        return CreateFiscalDeviceCommandHandler.Map(device);
    }
}
