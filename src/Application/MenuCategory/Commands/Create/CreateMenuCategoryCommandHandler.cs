using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.MenuCategories.Commands.Create;

public class CreateMenuCategoryCommandHandler
    : IRequestHandler<CreateMenuCategoryCommand, int>
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateMenuCategoryCommandHandler(
        IMenuCategoryRepository menuCategoryRepository,
        ICurrentUserService currentUserService)
    {
        _menuCategoryRepository = menuCategoryRepository;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(CreateMenuCategoryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        if (companyId == 0)
            throw new BadRequestException("CompanyId tapılmadı.");

        var exists = await _menuCategoryRepository.ExistsByNameAsync(
            companyId,
            request.Request.Name,
            cancellationToken);

        if (exists)
            throw new BadRequestException("Bu adda menu kateqoriyası artıq mövcuddur.");

        var entity = new MenuCategory
        {
            CompanyId = companyId,
            Name = request.Request.Name.Trim(),
            Description = request.Request.Description,
            IsActive = true
        };

        await _menuCategoryRepository.AddAsync(entity, cancellationToken);
        await _menuCategoryRepository.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}