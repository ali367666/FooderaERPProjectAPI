using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuCategories.Dtos;
using MediatR;

namespace Application.MenuCategories.Queries.GetAll;

public class GetAllMenuCategoriesQueryHandler
    : IRequestHandler<GetAllMenuCategoriesQuery, List<MenuCategoryResponse>>
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly ICurrentUserService _currentUserService;


    public GetAllMenuCategoriesQueryHandler(IMenuCategoryRepository menuCategoryRepository, ICurrentUserService currentUserService)
    {
        _menuCategoryRepository = menuCategoryRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<MenuCategoryResponse>> Handle(GetAllMenuCategoriesQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var entities = await _menuCategoryRepository.GetAllAsync(companyId, cancellationToken);

        return entities.Select(x => new MenuCategoryResponse
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            IsActive = x.IsActive
        }).ToList();
    }
}