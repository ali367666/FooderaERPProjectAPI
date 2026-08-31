using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.MenuItems.Queries.GetStockInfo;

public record GetMenuItemStockInfoQuery(int MenuItemId) : IRequest<MenuItemStockInfoResponse>;

public class MenuItemStockInfoResponse
{
    public int? StockItemId { get; set; }
    public string? StockItemName { get; set; }
    public List<WarehouseQuantityLine> DirectBalances { get; set; } = new();
    public List<WarehouseQuantityLine> RecipeMakeablePortions { get; set; } = new();
}

public class WarehouseQuantityLine
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public decimal Quantity { get; set; }
}

public class GetMenuItemStockInfoQueryHandler : IRequestHandler<GetMenuItemStockInfoQuery, MenuItemStockInfoResponse>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuItemRecipeRepository _menuItemRecipeRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMenuItemStockInfoQueryHandler(
        IMenuItemRepository menuItemRepository,
        IMenuItemRecipeRepository menuItemRecipeRepository,
        IWarehouseStockRepository warehouseStockRepository,
        ICurrentUserService currentUserService)
    {
        _menuItemRepository = menuItemRepository;
        _menuItemRecipeRepository = menuItemRecipeRepository;
        _warehouseStockRepository = warehouseStockRepository;
        _currentUserService = currentUserService;
    }

    public async Task<MenuItemStockInfoResponse> Handle(GetMenuItemStockInfoQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var menuItem = await _menuItemRepository.GetByIdAsync(request.MenuItemId, companyId, cancellationToken);
        if (menuItem is null)
            throw new Exception("Menu məhsulu tapılmadı.");

        var response = new MenuItemStockInfoResponse
        {
            StockItemId = menuItem.StockItemId,
            StockItemName = menuItem.StockItem?.Name
        };

        if (menuItem.StockItemId.HasValue)
        {
            var directRows = await _warehouseStockRepository.SearchAsync(
                companyId, null, menuItem.StockItemId.Value, null, cancellationToken);

            response.DirectBalances = directRows
                .Select(x => new WarehouseQuantityLine
                {
                    WarehouseId = x.WarehouseId,
                    WarehouseName = x.Warehouse.Name,
                    Quantity = x.Quantity
                })
                .ToList();
        }

        var recipeLines = await _menuItemRecipeRepository.GetByMenuItemIdAsync(companyId, request.MenuItemId, cancellationToken);
        if (recipeLines.Count > 0)
        {
            var warehouseNames = new Dictionary<int, string>();
            var portionsByWarehouse = new Dictionary<int, int>();

            foreach (var line in recipeLines)
            {
                if (line.QuantityPerPortion <= 0)
                    continue;

                var balances = await _warehouseStockRepository.SearchAsync(
                    companyId, null, line.StockItemId, null, cancellationToken);

                var balanceByWarehouse = balances.ToDictionary(x => x.WarehouseId, x => x.Quantity);
                foreach (var b in balances)
                    warehouseNames[b.WarehouseId] = b.Warehouse.Name;

                var warehouseIds = portionsByWarehouse.Count == 0
                    ? balanceByWarehouse.Keys.ToList()
                    : portionsByWarehouse.Keys.Union(balanceByWarehouse.Keys).ToList();

                foreach (var warehouseId in warehouseIds)
                {
                    var available = balanceByWarehouse.TryGetValue(warehouseId, out var qty) ? qty : 0;
                    var makeable = (int)Math.Floor(available / line.QuantityPerPortion);

                    portionsByWarehouse[warehouseId] = portionsByWarehouse.TryGetValue(warehouseId, out var existing)
                        ? Math.Min(existing, makeable)
                        : makeable;
                }
            }

            response.RecipeMakeablePortions = portionsByWarehouse
                .Select(kv => new WarehouseQuantityLine
                {
                    WarehouseId = kv.Key,
                    WarehouseName = warehouseNames.TryGetValue(kv.Key, out var name) ? name : $"#{kv.Key}",
                    Quantity = kv.Value
                })
                .ToList();
        }

        return response;
    }
}
