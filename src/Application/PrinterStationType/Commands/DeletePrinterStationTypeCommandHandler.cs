using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.PrinterStationType.Dtos;
using MediatR;

namespace Application.PrinterStationType.Commands;

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
