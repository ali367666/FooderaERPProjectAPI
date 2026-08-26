using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.PrinterStationType.Dtos;
using MediatR;

namespace Application.PrinterStationType.Commands;

public class CreatePrinterStationTypeCommandHandler : IRequestHandler<CreatePrinterStationTypeCommand, PrinterStationTypeResponse>
{
    private readonly IPrinterStationTypeRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreatePrinterStationTypeCommandHandler(IPrinterStationTypeRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<PrinterStationTypeResponse> Handle(CreatePrinterStationTypeCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var name = request.Request.Name.Trim();

        if (await _repository.ExistsByNameAsync(companyId, name, null, cancellationToken))
            throw new Exception("Bu adda stansiya artıq mövcuddur.");

        var stationType = new Domain.Entities.PrinterStationType
        {
            CompanyId = companyId,
            Name = name,
            IsActive = request.Request.IsActive
        };

        await _repository.AddAsync(stationType, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(stationType);
    }

    internal static PrinterStationTypeResponse Map(Domain.Entities.PrinterStationType s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        IsActive = s.IsActive
    };
}

public class UpdatePrinterStationTypeCommandHandler : IRequestHandler<UpdatePrinterStationTypeCommand, PrinterStationTypeResponse>
{
    private readonly IPrinterStationTypeRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public UpdatePrinterStationTypeCommandHandler(IPrinterStationTypeRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<PrinterStationTypeResponse> Handle(UpdatePrinterStationTypeCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var stationType = await _repository.GetByIdAsync(dto.Id, companyId, cancellationToken);
        if (stationType is null)
            throw new Exception("Stansiya tapılmadı.");

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(companyId, name, stationType.Id, cancellationToken))
            throw new Exception("Bu adda stansiya artıq mövcuddur.");

        stationType.Name = name;
        stationType.IsActive = dto.IsActive;

        _repository.Update(stationType);
        await _repository.SaveChangesAsync(cancellationToken);

        return CreatePrinterStationTypeCommandHandler.Map(stationType);
    }
}

public class DeletePrinterStationTypeCommandHandler : IRequestHandler<DeletePrinterStationTypeCommand>
{
    private readonly IPrinterStationTypeRepository _repository;
    private readonly IPrinterRepository _printerRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeletePrinterStationTypeCommandHandler(
        IPrinterStationTypeRepository repository,
        IPrinterRepository printerRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _printerRepository = printerRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeletePrinterStationTypeCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var stationType = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (stationType is null)
            throw new Exception("Stansiya tapılmadı.");

        if (await _printerRepository.ExistsByStationTypeIdAsync(stationType.Id, cancellationToken))
            throw new Exception("Bu stansiyanı istifadə edən printer(lər) var — əvvəlcə onların stansiyasını dəyişin.");

        _repository.Delete(stationType);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
