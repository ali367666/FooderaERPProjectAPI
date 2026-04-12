using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuItems.Dtos;
using MediatR;

namespace Application.MenuItems.Queries.GetAll;

public class GetAllMenuItemsQueryHandler
    : IRequestHandler<GetAllMenuItemsQuery, List<MenuItemResponse>>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllMenuItemsQueryHandler(
        IMenuItemRepository menuItemRepository,
        ICurrentUserService currentUserService)
    {
        _menuItemRepository = menuItemRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<MenuItemResponse>> Handle(GetAllMenuItemsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        if (companyId == 0)
            throw new BadRequestException("CompanyId tapılmadı.");

        var entities = await _menuItemRepository.GetAllAsync(companyId, cancellationToken);

        return entities.Select(entity => new MenuItemResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Price = entity.Price,
            Portion = entity.Portion,
            IsActive = entity.IsActive,
            MenuCategoryId = entity.MenuCategoryId,
            MenuCategoryName = entity.MenuCategory.Name
        }).ToList();
    }
}