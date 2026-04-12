using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.MenuItems.Commands.Update;

public class UpdateMenuItemCommandHandler
    : IRequestHandler<UpdateMenuItemCommand>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        IMenuCategoryRepository menuCategoryRepository,
        ICurrentUserService currentUserService)
    {
        _menuItemRepository = menuItemRepository;
        _menuCategoryRepository = menuCategoryRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateMenuItemCommand request, CancellationToken cancellationToken)
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

        var categoryExists = await _menuCategoryRepository.ExistsByIdAsync(
            request.Request.MenuCategoryId,
            companyId,
            cancellationToken);

        if (!categoryExists)
            throw new NotFoundException("Menu kateqoriyası tapılmadı.");

        var duplicateExists = await _menuItemRepository.ExistsByNameAsync(
            companyId,
            request.Request.MenuCategoryId,
            request.Request.Name,
            cancellationToken);

        if (duplicateExists &&
            !(string.Equals(entity.Name, request.Request.Name, StringComparison.OrdinalIgnoreCase)
              && entity.MenuCategoryId == request.Request.MenuCategoryId))
        {
            throw new BadRequestException("Bu adda menu məhsulu artıq mövcuddur.");
        }

        entity.Name = request.Request.Name.Trim();
        entity.Description = request.Request.Description;
        entity.Price = request.Request.Price;
        entity.Portion = request.Request.Portion;
        entity.MenuCategoryId = request.Request.MenuCategoryId;
        entity.IsActive = request.Request.IsActive;

        _menuItemRepository.Update(entity);
        await _menuItemRepository.SaveChangesAsync(cancellationToken);
    }
}