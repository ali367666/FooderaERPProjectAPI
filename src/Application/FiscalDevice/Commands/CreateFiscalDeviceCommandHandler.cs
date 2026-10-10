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
