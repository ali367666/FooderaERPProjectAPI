using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.İnterfaces;
using Application.Printer.Dtos;
using MediatR;

namespace Application.Printer.Commands;

public class DeletePrinterCommandHandler : IRequestHandler<DeletePrinterCommand>
{
    private readonly IPrinterRepository _repository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeletePrinterCommandHandler(
        IPrinterRepository repository,
        IMenuItemRepository menuItemRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _menuItemRepository = menuItemRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeletePrinterCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var printer = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (printer is null)
            throw new Exception("Printer tapılmadı.");

        var linkedItems = (await _menuItemRepository.GetAllAsync(companyId, cancellationToken))
            .Where(x => x.PrinterId == printer.Id)
            .ToList();
        foreach (var item in linkedItems)
        {
            item.PrinterId = null;
            _menuItemRepository.Update(item);
        }
        if (linkedItems.Count > 0)
            await _menuItemRepository.SaveChangesAsync(cancellationToken);

        _repository.Delete(printer);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
