using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Application.Printer.Dtos;
using MediatR;

namespace Application.Printer.Commands;

public class CreatePrinterCommandHandler : IRequestHandler<CreatePrinterCommand, PrinterResponse>
{
    private readonly IPrinterRepository _repository;
    private readonly IPrinterStationTypeRepository _stationTypeRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreatePrinterCommandHandler(
        IPrinterRepository repository,
        IPrinterStationTypeRepository stationTypeRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _stationTypeRepository = stationTypeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PrinterResponse> Handle(CreatePrinterCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(dto.RestaurantId, name, null, cancellationToken))
            throw new Exception("Bu adda printer artıq mövcuddur.");

        var stationType = await _stationTypeRepository.GetByIdAsync(dto.StationTypeId, companyId, cancellationToken);
        if (stationType is null)
            throw new Exception("Stansiya tapılmadı.");

        if (dto.IsPrimary)
        {
            var currentPrimary = await _repository.GetPrimaryAsync(companyId, dto.RestaurantId, null, cancellationToken);
            if (currentPrimary is not null)
            {
                currentPrimary.IsPrimary = false;
                _repository.Update(currentPrimary);
            }
        }

        if (dto.IsChiefPrinter)
        {
            var currentChief = await _repository.GetChiefAsync(companyId, dto.RestaurantId, null, cancellationToken);
            if (currentChief is not null)
            {
                currentChief.IsChiefPrinter = false;
                _repository.Update(currentChief);
            }
        }

        var printer = new Domain.Entities.Printer
        {
            CompanyId = companyId,
            RestaurantId = dto.RestaurantId,
            Name = name,
            StationTypeId = stationType.Id,
            IpAddress = dto.IpAddress.Trim(),
            Port = dto.Port,
            IsActive = dto.IsActive,
            IsPrimary = dto.IsPrimary,
            IsChiefPrinter = dto.IsChiefPrinter
        };

        await _repository.AddAsync(printer, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        printer.StationType = stationType;
        return Map(printer);
    }

    internal static PrinterResponse Map(Domain.Entities.Printer p) => new()
    {
        Id = p.Id,
        RestaurantId = p.RestaurantId,
        Name = p.Name,
        StationTypeId = p.StationTypeId,
        StationTypeName = p.StationType?.Name ?? "",
        IpAddress = p.IpAddress,
        Port = p.Port,
        IsActive = p.IsActive,
        IsPrimary = p.IsPrimary,
        IsChiefPrinter = p.IsChiefPrinter
    };
}
