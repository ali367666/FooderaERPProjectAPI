using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Application.Printer.Dtos;
using MediatR;

namespace Application.Printer.Commands;

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

        if (dto.IsPrimary && !printer.IsPrimary)
        {
            var currentPrimary = await _repository.GetPrimaryAsync(companyId, printer.RestaurantId, printer.Id, cancellationToken);
            if (currentPrimary is not null)
            {
                currentPrimary.IsPrimary = false;
                _repository.Update(currentPrimary);
            }
        }

        if (dto.IsChiefPrinter && !printer.IsChiefPrinter)
        {
            var currentChief = await _repository.GetChiefAsync(companyId, printer.RestaurantId, printer.Id, cancellationToken);
            if (currentChief is not null)
            {
                currentChief.IsChiefPrinter = false;
                _repository.Update(currentChief);
            }
        }

        printer.Name = name;
        printer.StationTypeId = stationType.Id;
        printer.IpAddress = dto.IpAddress.Trim();
        printer.Port = dto.Port;
        printer.IsActive = dto.IsActive;
        printer.IsPrimary = dto.IsPrimary;
        printer.IsChiefPrinter = dto.IsChiefPrinter;

        _repository.Update(printer);
        await _repository.SaveChangesAsync(cancellationToken);

        printer.StationType = stationType;
        return CreatePrinterCommandHandler.Map(printer);
    }
}
