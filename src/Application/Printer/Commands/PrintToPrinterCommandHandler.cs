using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Application.Printer.Dtos;
using MediatR;

namespace Application.Printer.Commands;

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
