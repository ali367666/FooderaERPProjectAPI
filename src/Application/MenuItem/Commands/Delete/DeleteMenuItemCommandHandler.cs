using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.MenuItems.Commands.Delete;

public class DeleteMenuItemCommandHandler
    : IRequestHandler<DeleteMenuItemCommand>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        ICurrentUserService currentUserService)
    {
        _menuItemRepository = menuItemRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteMenuItemCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        if (companyId == 0)
            throw new BadRequestException("CompanyId tapılmadı.");

        var entity = await _menuItemRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (entity is null)
            throw new NotFoundException("Menu məhsulu tapılmadı.");

        _menuItemRepository.Delete(entity);
        await _menuItemRepository.SaveChangesAsync(cancellationToken);
    }
}