using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.PrinterStationType.Dtos;
using MediatR;

namespace Application.PrinterStationType.Commands;

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
