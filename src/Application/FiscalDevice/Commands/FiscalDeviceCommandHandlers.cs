using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.FiscalDevice.Dtos;
using MediatR;

namespace Application.FiscalDevice.Commands;

public class CreateFiscalDeviceCommandHandler : IRequestHandler<CreateFiscalDeviceCommand, FiscalDeviceResponse>
{
    private readonly IFiscalDeviceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreateFiscalDeviceCommandHandler(IFiscalDeviceRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<FiscalDeviceResponse> Handle(CreateFiscalDeviceCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(dto.RestaurantId, name, null, cancellationToken))
            throw new Exception("Bu adda fiskal cihaz artıq mövcuddur.");

        var device = new Domain.Entities.FiscalDevice
        {
            CompanyId = companyId,
            RestaurantId = dto.RestaurantId,
            Name = name,
            Provider = dto.Provider,
            ConnectionInfo = string.IsNullOrWhiteSpace(dto.ConnectionInfo) ? null : dto.ConnectionInfo.Trim(),
            IsActive = dto.IsActive
        };

        await _repository.AddAsync(device, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(device);
    }

    internal static FiscalDeviceResponse Map(Domain.Entities.FiscalDevice d) => new()
    {
        Id = d.Id,
        RestaurantId = d.RestaurantId,
        Name = d.Name,
        Provider = d.Provider,
        ConnectionInfo = d.ConnectionInfo,
        IsActive = d.IsActive
    };
}

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

public class DeleteFiscalDeviceCommandHandler : IRequestHandler<DeleteFiscalDeviceCommand>
{
    private readonly IFiscalDeviceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteFiscalDeviceCommandHandler(IFiscalDeviceRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteFiscalDeviceCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var device = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (device is null)
            throw new Exception("Fiskal cihaz tapılmadı.");

        _repository.Delete(device);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
