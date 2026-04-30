using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuItems.Dtos;
using MediatR;

namespace Application.MenuItems.Queries.GetById;

public class GetMenuItemByIdQueryHandler
    : IRequestHandler<GetMenuItemByIdQuery, MenuItemResponse>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMenuItemByIdQueryHandler(
        IMenuItemRepository menuItemRepository,
        ICurrentUserService currentUserService)
    {
        _menuItemRepository = menuItemRepository;
        _currentUserService = currentUserService;
    }

    public async Task<MenuItemResponse> Handle(GetMenuItemByIdQuery request, CancellationToken cancellationToken)
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

        return new MenuItemResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Price = entity.Price,
            Portion = entity.Portion,
            IsActive = entity.IsActive,
            MenuCategoryId = entity.MenuCategoryId,
            MenuCategoryName = entity.MenuCategory.Name,
            PreparationType = entity.PreparationType
        };
    }
}