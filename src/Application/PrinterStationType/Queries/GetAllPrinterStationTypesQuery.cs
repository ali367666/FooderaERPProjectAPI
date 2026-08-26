using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.PrinterStationType.Commands;
using Application.PrinterStationType.Dtos;
using MediatR;

namespace Application.PrinterStationType.Queries;

public record GetAllPrinterStationTypesQuery : IRequest<List<PrinterStationTypeResponse>>;

public class GetAllPrinterStationTypesQueryHandler : IRequestHandler<GetAllPrinterStationTypesQuery, List<PrinterStationTypeResponse>>
{
    private readonly IPrinterStationTypeRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllPrinterStationTypesQueryHandler(IPrinterStationTypeRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<PrinterStationTypeResponse>> Handle(GetAllPrinterStationTypesQuery request, CancellationToken cancellationToken)
    {
        var stationTypes = await _repository.GetAllByCompanyAsync(_currentUserService.CompanyId, cancellationToken);
        return stationTypes.Select(CreatePrinterStationTypeCommandHandler.Map).ToList();
    }
}
