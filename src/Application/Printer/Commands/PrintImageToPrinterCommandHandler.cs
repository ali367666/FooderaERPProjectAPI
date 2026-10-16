using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Application.Printer.Dtos;
using MediatR;

namespace Application.Printer.Commands;

public class PrintImageToPrinterCommandHandler : IRequestHandler<PrintImageToPrinterCommand>
{
    private readonly IPrinterRepository _repository;
    private readonly INetworkPrinterService _networkPrinterService;
    private readonly ICurrentUserService _currentUserService;

    public PrintImageToPrinterCommandHandler(
        IPrinterRepository repository,
        INetworkPrinterService networkPrinterService,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _networkPrinterService = networkPrinterService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(PrintImageToPrinterCommand request, CancellationToken cancellationToken)
    {
        if (request.WidthDots <= 0 || request.WidthDots % 8 != 0 || request.WidthDots > 1024
            || request.Height <= 0 || request.Height > 20000)
            throw new Exception("Çek şəklinin ölçüsü düzgün deyil.");

        byte[] bits;
        try
        {
            bits = Convert.FromBase64String(request.Data);
        }
        catch (FormatException)
        {
            throw new Exception("Çek şəkli zədələnib.");
        }
        if (bits.Length != request.WidthDots / 8 * request.Height)
            throw new Exception("Çek şəklinin ölçüsü məlumatla uyğun gəlmir.");

        var printer = await _repository.GetByIdAsync(request.PrinterId, _currentUserService.CompanyId, cancellationToken);
        if (printer is null)
            throw new Exception("Printer tapılmadı.");
        if (!printer.IsActive)
            throw new Exception("Bu printer deaktivdir.");

        await _networkPrinterService.PrintRasterAsync(
            printer.IpAddress, printer.Port, request.WidthDots, request.Height, bits, request.Trailer, cancellationToken);
    }
}
