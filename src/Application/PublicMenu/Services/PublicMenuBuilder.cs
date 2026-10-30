using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.PublicMenu.Dtos;
using Domain.Enums;

namespace Application.PublicMenu.Services;

public sealed class PublicMenuBuilder : IPublicMenuBuilder
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuItemRecipeRepository _menuItemRecipeRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;

    public PublicMenuBuilder(
        IMenuItemRepository menuItemRepository,
        IMenuItemRecipeRepository menuItemRecipeRepository,
        IWarehouseRepository warehouseRepository,
        IWarehouseStockRepository warehouseStockRepository)
    {
        _menuItemRepository = menuItemRepository;
        _menuItemRecipeRepository = menuItemRecipeRepository;
        _warehouseRepository = warehouseRepository;
        _warehouseStockRepository = warehouseStockRepository;
    }

    public async Task<List<PublicMenuCategoryResponse>> BuildCategoriesAsync(int companyId, CancellationToken cancellationToken)
    {
        var menuItems = await _menuItemRepository.GetAllAsync(companyId, cancellationToken);

        return menuItems
            .Where(x => x.IsActive && !x.HideFromPosSearch && x.MenuCategory.IsActive)
            .GroupBy(x => x.MenuCategory)
            .OrderBy(g => g.Key.Name)
            .Select(g => new PublicMenuCategoryResponse
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                ImageUrl = g.Key.ImageUrl,
                Items = g
                    .OrderBy(x => x.Name)
                    .Select(x => new PublicMenuItemResponse
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Description = x.Description,
                        ImageUrl = x.ImageUrl,
                        Price = x.Price,
                        Portion = x.Portion,
                        IsAvailable = true
                    })
                    .ToList()
            })
            .Where(c => c.Items.Count > 0)
            .ToList();
    }

    public string ComputeHash(IReadOnlyCollection<PublicMenuCategoryResponse> categories)
    {
        var json = JsonSerializer.Serialize(categories);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    public async Task<HashSet<int>> GetOutOfStockMenuItemIdsAsync(
        int companyId,
        int restaurantId,
        CancellationToken cancellationToken)
    {
        var result = new HashSet<int>();

        var warehouses = await _warehouseRepository.GetByRestaurantIdAsync(restaurantId, cancellationToken);
        var restaurantWarehouse = warehouses.FirstOrDefault(x => x.CompanyId == companyId && x.Type == WarehouseType.Restaurant)
            ?? warehouses.FirstOrDefault(x => x.CompanyId == companyId);

        if (restaurantWarehouse is null)
            return result;

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
                result.Add(menuItem.Id);
        }

        return result;
    }
}
