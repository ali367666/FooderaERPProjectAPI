using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.MenuItems.Queries.GetAvailability;

public record GetMenuItemAvailabilityQuery(int RestaurantId) : IRequest<MenuItemAvailabilityResponse>;

public class MenuItemAvailabilityResponse
{
    public List<int> OutOfStockMenuItemIds { get; set; } = new();
}

public class GetMenuItemAvailabilityQueryHandler : IRequestHandler<GetMenuItemAvailabilityQuery, MenuItemAvailabilityResponse>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuItemRecipeRepository _menuItemRecipeRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMenuItemAvailabilityQueryHandler(
        IMenuItemRepository menuItemRepository,
        IMenuItemRecipeRepository menuItemRecipeRepository,
        IWarehouseStockRepository warehouseStockRepository,
        IWarehouseRepository warehouseRepository,
        ICurrentUserService currentUserService)
    {
        _menuItemRepository = menuItemRepository;
        _menuItemRecipeRepository = menuItemRecipeRepository;
        _warehouseStockRepository = warehouseStockRepository;
        _warehouseRepository = warehouseRepository;
        _currentUserService = currentUserService;
    }

    public async Task<MenuItemAvailabilityResponse> Handle(
        GetMenuItemAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var response = new MenuItemAvailabilityResponse();

        var warehouses = await _warehouseRepository.GetByRestaurantIdAsync(request.RestaurantId, cancellationToken);
        var restaurantWarehouse = warehouses.FirstOrDefault(x => x.CompanyId == companyId && x.Type == WarehouseType.Restaurant)
            ?? warehouses.FirstOrDefault(x => x.CompanyId == companyId);

        if (restaurantWarehouse is null)
            return response;

        var balances = await _warehouseStockRepository.SearchAsync(
            companyId, restaurantWarehouse.Id, null, null, cancellationToken);
        var balanceByStockItem = balances.ToDictionary(x => x.StockItemId, x => x.Quantity);

        var menuItems = await _menuItemRepository.GetAllAsync(companyId, cancellationToken);
        var recipeLinesByMenuItem = (await _menuItemRecipeRepository.GetAllAsync(companyId, cancellationToken))
            .GroupBy(x => x.MenuItemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var menuItem in menuItems.Where(x => x.IsActive))
        {
            List<(int StockItemId, decimal QuantityPerPortion)> effectiveLines;

            if (recipeLinesByMenuItem.TryGetValue(menuItem.Id, out var recipeLines) && recipeLines.Count > 0)
            {
                effectiveLines = recipeLines
                    .Select(x => (x.StockItemId, x.QuantityPerPortion))
                    .ToList();
            }
            else if (menuItem.StockItemId.HasValue)
            {
                effectiveLines = new List<(int, decimal)> { (menuItem.StockItemId.Value, 1m) };
            }
            else
            {
                continue;
            }

            var makeablePortions = effectiveLines
                .Where(l => l.QuantityPerPortion > 0)
                .Select(l => balanceByStockItem.TryGetValue(l.StockItemId, out var qty)
                    ? Math.Floor(qty / l.QuantityPerPortion)
                    : 0)
                .DefaultIfEmpty(0)
                .Min();

            if (makeablePortions < 1)
                response.OutOfStockMenuItemIds.Add(menuItem.Id);
        }

        return response;
    }
}
