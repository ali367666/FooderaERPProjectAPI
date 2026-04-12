using Application.Common.Exceptions;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.MenuCategories.Commands.Update;

public class UpdateMenuCategoryCommandHandler
    : IRequestHandler<UpdateMenuCategoryCommand>
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;

    public UpdateMenuCategoryCommandHandler(IMenuCategoryRepository menuCategoryRepository)
    {
        _menuCategoryRepository = menuCategoryRepository;
    }

    public async Task Handle(UpdateMenuCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _menuCategoryRepository.GetByIdAsync(
            request.Id,
            request.CompanyId,
            cancellationToken);

        if (entity is null)
            throw new NotFoundException("Menu kateqoriyası tapılmadı.");

        var duplicateExists = await _menuCategoryRepository.ExistsByNameAsync(
            request.CompanyId,
            request.Request.Name,
            cancellationToken);

        if (duplicateExists && !string.Equals(entity.Name, request.Request.Name, StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("Bu adda menu kateqoriyası artıq mövcuddur.");

        entity.Name = request.Request.Name.Trim();
        entity.Description = request.Request.Description;
        entity.IsActive = request.Request.IsActive;

        _menuCategoryRepository.Update(entity);
        await _menuCategoryRepository.SaveChangesAsync(cancellationToken);
    }
}