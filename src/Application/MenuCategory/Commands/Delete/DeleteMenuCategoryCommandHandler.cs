using Application.Common.Exceptions;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.MenuCategories.Commands.Delete;

public class DeleteMenuCategoryCommandHandler
    : IRequestHandler<DeleteMenuCategoryCommand>
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;

    public DeleteMenuCategoryCommandHandler(IMenuCategoryRepository menuCategoryRepository)
    {
        _menuCategoryRepository = menuCategoryRepository;
    }

    public async Task Handle(DeleteMenuCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _menuCategoryRepository.GetByIdAsync(
            request.Id,
            request.CompanyId,
            cancellationToken);

        if (entity is null)
            throw new NotFoundException("Menu kateqoriyası tapılmadı.");

        _menuCategoryRepository.Delete(entity);
        await _menuCategoryRepository.SaveChangesAsync(cancellationToken);
    }
}