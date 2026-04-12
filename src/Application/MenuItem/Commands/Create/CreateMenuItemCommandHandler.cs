using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.MenuItems.Commands.Create;

public class CreateMenuItemCommandHandler
    : IRequestHandler<CreateMenuItemCommand, int>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        IMenuCategoryRepository menuCategoryRepository,
        ICurrentUserService currentUserService)
    {
        _menuItemRepository = menuItemRepository;
        _menuCategoryRepository = menuCategoryRepository;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        if (companyId == 0)
            throw new BadRequestException("CompanyId tapılmadı.");

        var categoryExists = await _menuCategoryRepository.ExistsByIdAsync(
            request.Request.MenuCategoryId,
            companyId,
            cancellationToken);

        if (!categoryExists)
            throw new NotFoundException("Menu kateqoriyası tapılmadı.");

        var exists = await _menuItemRepository.ExistsByNameAsync(
            companyId,
            request.Request.MenuCategoryId,
            request.Request.Name,
            cancellationToken);

        if (exists)
            throw new BadRequestException("Bu adda menu məhsulu artıq mövcuddur.");

        var entity = new MenuItem
        {
            CompanyId = companyId,
            Name = request.Request.Name.Trim(),
            Description = request.Request.Description,
            Price = request.Request.Price,
            Portion = request.Request.Portion,
            MenuCategoryId = request.Request.MenuCategoryId,
            IsActive = true
        };

        await _menuItemRepository.AddAsync(entity, cancellationToken);
        await _menuItemRepository.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}