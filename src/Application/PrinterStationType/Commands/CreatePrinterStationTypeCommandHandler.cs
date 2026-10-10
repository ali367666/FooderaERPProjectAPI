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
