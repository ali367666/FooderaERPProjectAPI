using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuItemRecipes.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.MenuItemRecipes.Queries.GetAll;

public class GetAllMenuItemRecipeLinesQueryHandler
    : IRequestHandler<GetAllMenuItemRecipeLinesQuery, List<MenuItemRecipeResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMenuItemRecipeRepository _menuItemRecipeRepository;
    private readonly ILogger<GetAllMenuItemRecipeLinesQueryHandler> _logger;

    public GetAllMenuItemRecipeLinesQueryHandler(
        ICurrentUserService currentUserService,
        IMenuItemRecipeRepository menuItemRecipeRepository,
        ILogger<GetAllMenuItemRecipeLinesQueryHandler> logger)
    {
        _currentUserService = currentUserService;
        _menuItemRecipeRepository = menuItemRecipeRepository;
        _logger = logger;
    }

    public async Task<List<MenuItemRecipeResponse>> Handle(
        GetAllMenuItemRecipeLinesQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var rows = await _menuItemRecipeRepository.GetAllAsync(companyId, cancellationToken);
        _logger.LogInformation("MenuItemRecipes.GetAll companyId={CompanyId} linesCount={Count}", companyId, rows.Count);

        return rows
            .GroupBy(x => new { x.MenuItemId, x.MenuItem.Name })
            .Select(g => new MenuItemRecipeResponse
            {
                Id = g.Min(x => x.Id),
                MenuItemId = g.Key.MenuItemId,
                MenuItemName = g.Key.Name,
                Lines = g.Select(x => new MenuItemRecipeIngredientLineResponse
                {
                    StockItemId = x.StockItemId,
                    StockItemName = x.StockItem.Name,
                    Quantity = x.QuantityPerPortion,
                    Unit = x.StockItem.Unit.ToString(),
                }).ToList()
            })
            .OrderBy(x => x.MenuItemName)
            .ToList();
    }
}
