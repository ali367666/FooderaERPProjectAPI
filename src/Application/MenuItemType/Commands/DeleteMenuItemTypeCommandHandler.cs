using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuItemType.Dtos;
using MediatR;

namespace Application.MenuItemType.Commands;

public class DeleteMenuItemTypeCommandHandler : IRequestHandler<DeleteMenuItemTypeCommand>
{
    private readonly IMenuItemTypeRepository _repository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteMenuItemTypeCommandHandler(
        IMenuItemTypeRepository repository,
        IMenuItemRepository menuItemRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _menuItemRepository = menuItemRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteMenuItemTypeCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var itemType = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (itemType is null)
            throw new Exception("Məhsul növü tapılmadı.");

        if (await _menuItemRepository.ExistsByItemTypeIdAsync(itemType.Id, cancellationToken))
            throw new Exception("Bu növü istifadə edən məhsul(lar) var — əvvəlcə onların növünü dəyişin.");

        _repository.Delete(itemType);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
