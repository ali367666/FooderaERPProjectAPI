using Application.Common.Exceptions;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuCategories.Dtos;
using MediatR;

namespace Application.MenuCategories.Queries.GetById;

public class GetMenuCategoryByIdQueryHandler
    : IRequestHandler<GetMenuCategoryByIdQuery, MenuCategoryResponse>
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;

    public GetMenuCategoryByIdQueryHandler(IMenuCategoryRepository menuCategoryRepository)
    {
        _menuCategoryRepository = menuCategoryRepository;
    }

    public async Task<MenuCategoryResponse> Handle(GetMenuCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _menuCategoryRepository.GetByIdAsync(
            request.Id,
            request.CompanyId,
            cancellationToken);

        if (entity is null)
            throw new NotFoundException("Menu kateqoriyası tapılmadı.");

        return new MenuCategoryResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }
}