using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuItemRecipes.Dtos;
using MediatR;

namespace Application.MenuItemRecipes.Queries.GetByMenuItemId;

public class GetMenuItemRecipeByMenuItemIdQueryHandler
    : IRequestHandler<GetMenuItemRecipeByMenuItemIdQuery, MenuItemRecipeResponse?>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuItemRecipeRepository _menuItemRecipeRepository;

    public GetMenuItemRecipeByMenuItemIdQueryHandler(
        ICurrentUserService currentUserService,
        IMenuItemRepository menuItemRepository,
        IMenuItemRecipeRepository menuItemRecipeRepository)
    {
        _currentUserService = currentUserService;
        _menuItemRepository = menuItemRepository;
        _menuItemRecipeRepository = menuItemRecipeRepository;
    }

    public async Task<MenuItemRecipeResponse?> Handle(
        GetMenuItemRecipeByMenuItemIdQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var menuItem = await _menuItemRepository.GetByIdAsync(request.MenuItemId, companyId, cancellationToken);
        if (menuItem is null)
            throw new Exception("Menu item not found.");

        var rows = await _menuItemRecipeRepository.GetByMenuItemIdAsync(companyId, request.MenuItemId, cancellationToken);
        return new MenuItemRecipeResponse
        {
            Id = rows.Count > 0 ? rows.Min(x => x.Id) : 0,
            MenuItemId = menuItem.Id,
            MenuItemName = menuItem.Name,
            Lines = rows.Select(x => new MenuItemRecipeIngredientLineResponse
            {
                StockItemId = x.StockItemId,
                StockItemName = x.StockItem.Name,
                Quantity = x.QuantityPerPortion,
                Unit = x.StockItem.Unit.ToString(),
            }).ToList()
        };
    }
}
