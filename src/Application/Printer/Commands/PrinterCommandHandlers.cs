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

        var printer = new Domain.Entities.Printer
        {
            CompanyId = companyId,
            RestaurantId = dto.RestaurantId,
            Name = name,
            StationTypeId = stationType.Id,
            IpAddress = dto.IpAddress.Trim(),
            Port = dto.Port,
            IsActive = dto.IsActive
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
        IsActive = p.IsActive
    };
}

public class UpdatePrinterCommandHandler : IRequestHandler<UpdatePrinterCommand, PrinterResponse>
{
    private readonly IPrinterRepository _repository;
    private readonly IPrinterStationTypeRepository _stationTypeRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdatePrinterCommandHandler(
        IPrinterRepository repository,
        IPrinterStationTypeRepository stationTypeRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _stationTypeRepository = stationTypeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PrinterResponse> Handle(UpdatePrinterCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var printer = await _repository.GetByIdAsync(dto.Id, companyId, cancellationToken);
        if (printer is null)
            throw new Exception("Printer tapılmadı.");

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(printer.RestaurantId, name, printer.Id, cancellationToken))
            throw new Exception("Bu adda printer artıq mövcuddur.");

        var stationType = await _stationTypeRepository.GetByIdAsync(dto.StationTypeId, companyId, cancellationToken);
        if (stationType is null)
            throw new Exception("Stansiya tapılmadı.");

        printer.Name = name;
        printer.StationTypeId = stationType.Id;
        printer.IpAddress = dto.IpAddress.Trim();
        printer.Port = dto.Port;
        printer.IsActive = dto.IsActive;

        _repository.Update(printer);
        await _repository.SaveChangesAsync(cancellationToken);

        printer.StationType = stationType;
        return CreatePrinterCommandHandler.Map(printer);
    }
}

public class DeletePrinterCommandHandler : IRequestHandler<DeletePrinterCommand>
{
    private readonly IPrinterRepository _repository;
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeletePrinterCommandHandler(
        IPrinterRepository repository,
        IMenuCategoryRepository menuCategoryRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _menuCategoryRepository = menuCategoryRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeletePrinterCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var printer = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (printer is null)
            throw new Exception("Printer tapılmadı.");

        var linkedCategories = (await _menuCategoryRepository.GetAllAsync(companyId, cancellationToken))
            .Where(c => c.PrinterId == printer.Id)
            .ToList();
        foreach (var category in linkedCategories)
        {
            category.PrinterId = null;
            _menuCategoryRepository.Update(category);
        }
        if (linkedCategories.Count > 0)
            await _menuCategoryRepository.SaveChangesAsync(cancellationToken);

        _repository.Delete(printer);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}

public class PrintToPrinterCommandHandler : IRequestHandler<PrintToPrinterCommand>
{
    private readonly IPrinterRepository _repository;
    private readonly INetworkPrinterService _networkPrinterService;
    private readonly ICurrentUserService _currentUserService;

    public PrintToPrinterCommandHandler(
        IPrinterRepository repository,
        INetworkPrinterService networkPrinterService,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _networkPrinterService = networkPrinterService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(PrintToPrinterCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var printer = await _repository.GetByIdAsync(request.PrinterId, companyId, cancellationToken);
        if (printer is null)
            throw new Exception("Printer tapılmadı.");
        if (!printer.IsActive)
            throw new Exception("Bu printer deaktivdir.");

        await _networkPrinterService.PrintAsync(printer.IpAddress, printer.Port, request.Content, cancellationToken);
    }
}
