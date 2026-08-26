using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Printer.Commands;
using Application.Printer.Dtos;
using MediatR;

namespace Application.Printer.Queries;

public record GetAllPrintersQuery(int RestaurantId) : IRequest<List<PrinterResponse>>;

public class GetAllPrintersQueryHandler : IRequestHandler<GetAllPrintersQuery, List<PrinterResponse>>
{
    private readonly IPrinterRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllPrintersQueryHandler(IPrinterRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<PrinterResponse>> Handle(GetAllPrintersQuery request, CancellationToken cancellationToken)
    {
        var printers = await _repository.GetAllByRestaurantAsync(_currentUserService.CompanyId, request.RestaurantId, cancellationToken);
        return printers.Select(CreatePrinterCommandHandler.Map).ToList();
    }
}
