using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.MenuItemRecipes.Dtos;
using Domain.Entities.WarehouseAndStock;
using MediatR;

namespace Application.MenuItemRecipes.Commands.Upsert;

public class UpsertMenuItemRecipeCommandHandler
    : IRequestHandler<UpsertMenuItemRecipeCommand, MenuItemRecipeResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IMenuItemRecipeRepository _menuItemRecipeRepository;

    public UpsertMenuItemRecipeCommandHandler(
        ICurrentUserService currentUserService,
        IMenuItemRepository menuItemRepository,
        IStockItemRepository stockItemRepository,
        IMenuItemRecipeRepository menuItemRecipeRepository)
    {
        _currentUserService = currentUserService;
        _menuItemRepository = menuItemRepository;
        _stockItemRepository = stockItemRepository;
        _menuItemRecipeRepository = menuItemRecipeRepository;
    }

    public async Task<MenuItemRecipeResponse> Handle(
        UpsertMenuItemRecipeCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var menuItem = await _menuItemRepository.GetByIdAsync(request.MenuItemId, companyId, cancellationToken);
        if (menuItem is null)
            throw new Exception("Menu item not found.");

        var lines = request.Request.Lines ?? [];
        if (lines.GroupBy(x => x.StockItemId).Any(g => g.Count() > 1))
            throw new Exception("Recipe cannot contain duplicate stock items.");
        if (lines.Any(x => (x.QuantityPerPortion <= 0) && (x.Quantity <= 0)))
            throw new Exception("Quantity must be greater than zero.");

        var requestedStockItemIds = lines.Select(x => x.StockItemId).Distinct().ToList();
        var existingStockItems = new Dictionary<int, Domain.Entities.WarehouseAndStock.StockItem>();
        foreach (var stockItemId in requestedStockItemIds)
        {
            var stockItem = await _stockItemRepository.GetByIdAsync(stockItemId, cancellationToken);
            if (stockItem is null || stockItem.CompanyId != companyId)
                throw new Exception($"Stock item not found: {stockItemId}");
            existingStockItems[stockItemId] = stockItem;
        }

        var existing = await _menuItemRecipeRepository.GetByMenuItemIdAsync(companyId, request.MenuItemId, cancellationToken);
        var existingByStockItemId = existing.ToDictionary(x => x.StockItemId, x => x);
        var requestStockItemIds = lines.Select(x => x.StockItemId).ToHashSet();

        // Update existing lines or add new lines.
        var toAdd = new List<MenuItemRecipeLine>();
        foreach (var line in lines)
        {
            var stockItem = existingStockItems[line.StockItemId];
            var quantityPerPortion = line.QuantityPerPortion > 0 ? line.QuantityPerPortion : line.Quantity;
            var unit = line.Unit ?? stockItem.Unit;

            if (!AreUnitsCompatible(unit, stockItem.Unit))
                throw new Exception($"Unit {unit} is not compatible with stock item unit {stockItem.Unit}.");

            if (existingByStockItemId.TryGetValue(line.StockItemId, out var existingLine))
            {
                existingLine.QuantityPerPortion = quantityPerPortion;
                existingLine.Unit = unit;
            }
            else
            {
                toAdd.Add(new MenuItemRecipeLine
                {
                    CompanyId = companyId,
                    MenuItemId = request.MenuItemId,
                    StockItemId = line.StockItemId,
                    QuantityPerPortion = quantityPerPortion,
                    Unit = unit,
                });
            }
        }

        // Remove only lines that were explicitly removed from UI payload.
        var toRemove = existing.Where(x => !requestStockItemIds.Contains(x.StockItemId)).ToList();
        if (toRemove.Count > 0)
            _menuItemRecipeRepository.RemoveRange(toRemove);
        if (toAdd.Count > 0)
            await _menuItemRecipeRepository.AddRangeAsync(toAdd, cancellationToken);

        await _menuItemRecipeRepository.SaveChangesAsync(cancellationToken);

        var saved = await _menuItemRecipeRepository.GetByMenuItemIdAsync(companyId, request.MenuItemId, cancellationToken);
        return new MenuItemRecipeResponse
        {
            Id = saved.Count > 0 ? saved.Min(x => x.Id) : 0,
            MenuItemId = menuItem.Id,
            MenuItemName = menuItem.Name,
            Lines = saved.Select(x => new MenuItemRecipeIngredientLineResponse
            {
                StockItemId = x.StockItemId,
                StockItemName = x.StockItem.Name,
                Quantity = x.QuantityPerPortion,
                Unit = x.StockItem.Unit.ToString(),
            }).ToList()
        };
    }

    private static bool AreUnitsCompatible(Domain.Enums.UnitOfMeasure from, Domain.Enums.UnitOfMeasure to)
    {
        if (from == to) return true;
        return (from, to) switch
        {
            (Domain.Enums.UnitOfMeasure.Kg, Domain.Enums.UnitOfMeasure.Gram) => true,
            (Domain.Enums.UnitOfMeasure.Gram, Domain.Enums.UnitOfMeasure.Kg) => true,
            (Domain.Enums.UnitOfMeasure.Liter, Domain.Enums.UnitOfMeasure.Ml) => true,
            (Domain.Enums.UnitOfMeasure.Ml, Domain.Enums.UnitOfMeasure.Liter) => true,
            _ => false
        };
    }

}
