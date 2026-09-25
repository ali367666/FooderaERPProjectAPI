using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuCategories.Dtos;
using MediatR;

namespace Application.MenuCategories.Queries.GetById;

public class GetMenuCategoryByIdQueryHandler
    : IRequestHandler<GetMenuCategoryByIdQuery, MenuCategoryResponse>
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMenuCategoryByIdQueryHandler(
        IMenuCategoryRepository menuCategoryRepository,
        ICurrentUserService currentUserService)
    {
        _menuCategoryRepository = menuCategoryRepository;
        _currentUserService = currentUserService;
    }

    public async Task<MenuCategoryResponse> Handle(GetMenuCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.IsSuperAdmin ? request.CompanyId : _currentUserService.CompanyId;

        var entity = await _menuCategoryRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (entity is null)
            throw new NotFoundException("Menu kateqoriyası tapılmadı.");

        return new MenuCategoryResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ImageUrl = entity.ImageUrl,
            IsActive = entity.IsActive,
            ParentCategoryId = entity.ParentCategoryId,
            ParentCategoryName = entity.ParentCategory?.Name
        };
    }
}