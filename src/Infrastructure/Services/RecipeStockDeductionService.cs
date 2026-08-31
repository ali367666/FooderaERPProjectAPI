using Application.Common.Exceptions;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Domain.Entities;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class RecipeStockDeductionService : IRecipeStockDeductionService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMenuItemRecipeRepository _menuItemRecipeRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly AppDbContext _context;

    public RecipeStockDeductionService(
        IOrderRepository orderRepository,
        IMenuItemRecipeRepository menuItemRecipeRepository,
        IWarehouseRepository warehouseRepository,
        IStockMovementRepository stockMovementRepository,
        AppDbContext context)
    {
        _orderRepository = orderRepository;
        _menuItemRecipeRepository = menuItemRecipeRepository;
        _warehouseRepository = warehouseRepository;
        _stockMovementRepository = stockMovementRepository;
        _context = context;
    }

    private async Task<List<MenuItemRecipeLine>> ResolveRecipeLinesAsync(
        int companyId, int menuItemId, CancellationToken cancellationToken)
    {
        var recipeLines = await _menuItemRecipeRepository.GetByMenuItemIdAsync(companyId, menuItemId, cancellationToken);
        if (recipeLines.Count > 0)
            return recipeLines;

        var menuItem = await _context.MenuItems.AsNoTracking().FirstOrDefaultAsync(
            x => x.Id == menuItemId && x.CompanyId == companyId,
            cancellationToken);

        if (menuItem?.StockItemId is null)
            return new List<MenuItemRecipeLine>();

        return new List<MenuItemRecipeLine>
        {
            new()
            {
                CompanyId = companyId,
                MenuItemId = menuItemId,
                StockItemId = menuItem.StockItemId.Value,
                QuantityPerPortion = 1
            }
        };
    }

    private async Task<Domain.Entities.Warehouse> ResolveRestaurantWarehouseAsync(
        int companyId, int restaurantId, CancellationToken cancellationToken)
    {
        var warehouses = await _warehouseRepository.GetByRestaurantIdAsync(restaurantId, cancellationToken);
        var restaurantWarehouse = warehouses.FirstOrDefault(x => x.CompanyId == companyId && x.Type == WarehouseType.Restaurant)
            ?? warehouses.FirstOrDefault(x => x.CompanyId == companyId);

        if (restaurantWarehouse is null)
            throw new BadRequestException("Restaurant warehouse was not found.");

        return restaurantWarehouse;
    }

    private static Dictionary<int, decimal> ComputeRequiredByStockItem(
        List<MenuItemRecipeLine> recipeLines, int quantity)
    {
        var requiredByStockItem = new Dictionary<int, decimal>();
        foreach (var recipeLine in recipeLines)
        {
            var requiredQuantity = recipeLine.QuantityPerPortion * quantity;
            if (!requiredByStockItem.TryAdd(recipeLine.StockItemId, requiredQuantity))
                requiredByStockItem[recipeLine.StockItemId] += requiredQuantity;
        }
        return requiredByStockItem;
    }

    public async Task DeductForOrderLineAsync(OrderLine orderLine, CancellationToken cancellationToken)
    {
        if (orderLine.IsStockDeducted)
            return;

        var companyId = orderLine.CompanyId;
        var order = await _orderRepository.GetByIdAsync(orderLine.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new NotFoundException("Order not found.");

        var recipeLines = await ResolveRecipeLinesAsync(companyId, orderLine.MenuItemId, cancellationToken);
        if (recipeLines.Count == 0)
            return;

        var restaurantWarehouse = await ResolveRestaurantWarehouseAsync(companyId, order.RestaurantId, cancellationToken);
        var requiredByStockItem = ComputeRequiredByStockItem(recipeLines, orderLine.Quantity);

        foreach (var req in requiredByStockItem)
        {
            var balance = await _context.WarehouseStocks.FirstOrDefaultAsync(
                x => x.CompanyId == companyId
                    && x.WarehouseId == restaurantWarehouse.Id
                    && x.StockItemId == req.Key,
                cancellationToken);
            if (balance is null)
            {
                var itemName = recipeLines.FirstOrDefault(x => x.StockItemId == req.Key)?.StockItem?.Name ?? $"StockItem#{req.Key}";
                throw new BadRequestException($"Anbarda stok tapılmadı: {itemName}");
            }
            if (balance.Quantity < req.Value)
            {
                var itemName = recipeLines.FirstOrDefault(x => x.StockItemId == req.Key)?.StockItem?.Name ?? $"StockItem#{req.Key}";
                throw new BadRequestException($"Anbarda kifayət qədər stok yoxdur: {itemName}");
            }
        }

        var now = DateTime.UtcNow;
        foreach (var req in requiredByStockItem)
        {
            var balance = await _context.WarehouseStocks.FirstOrDefaultAsync(
                x => x.CompanyId == companyId
                    && x.WarehouseId == restaurantWarehouse.Id
                    && x.StockItemId == req.Key,
                cancellationToken);
            if (balance is null)
                continue;

            balance.Quantity -= req.Value;
            balance.LastModifiedAtUtc = now;
            _context.WarehouseStocks.Update(balance);

            await _stockMovementRepository.AddAsync(
                new StockMovement
                {
                    CompanyId = companyId,
                    WarehouseId = restaurantWarehouse.Id,
                    FromWarehouseId = restaurantWarehouse.Id,
                    ToWarehouseId = null,
                    StockItemId = req.Key,
                    Type = StockMovementType.OrderConsumptionOut,
                    SourceType = StockMovementSourceType.Order,
                    SourceId = order.Id,
                    SourceDocumentNo = order.OrderNumber,
                    MovementDate = now,
                    Quantity = req.Value,
                    Note = $"Order recipe consumption: {order.OrderNumber}"
                },
                cancellationToken);
        }

        orderLine.IsStockDeducted = true;
    }

    public async Task RestoreForOrderLineAsync(OrderLine orderLine, CancellationToken cancellationToken)
    {
        if (!orderLine.IsStockDeducted)
            return;

        var companyId = orderLine.CompanyId;
        var order = await _orderRepository.GetByIdAsync(orderLine.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new NotFoundException("Order not found.");

        var recipeLines = await ResolveRecipeLinesAsync(companyId, orderLine.MenuItemId, cancellationToken);
        if (recipeLines.Count == 0)
        {
            orderLine.IsStockDeducted = false;
            return;
        }

        var restaurantWarehouse = await ResolveRestaurantWarehouseAsync(companyId, order.RestaurantId, cancellationToken);
        var requiredByStockItem = ComputeRequiredByStockItem(recipeLines, orderLine.Quantity);

        var now = DateTime.UtcNow;
        foreach (var req in requiredByStockItem)
        {
            var balance = await _context.WarehouseStocks.FirstOrDefaultAsync(
                x => x.CompanyId == companyId
                    && x.WarehouseId == restaurantWarehouse.Id
                    && x.StockItemId == req.Key,
                cancellationToken);

            if (balance is null)
            {
                balance = new Domain.Entities.WarehouseAndStock.WarehouseStock
                {
                    CompanyId = companyId,
                    WarehouseId = restaurantWarehouse.Id,
                    StockItemId = req.Key,
                    Quantity = 0,
                    UnitId = 0,
                    CreatedAtUtc = now,
                };
                await _context.WarehouseStocks.AddAsync(balance, cancellationToken);
            }

            balance.Quantity += req.Value;
            balance.LastModifiedAtUtc = now;

            await _stockMovementRepository.AddAsync(
                new StockMovement
                {
                    CompanyId = companyId,
                    WarehouseId = restaurantWarehouse.Id,
                    FromWarehouseId = null,
                    ToWarehouseId = restaurantWarehouse.Id,
                    StockItemId = req.Key,
                    Type = StockMovementType.OrderConsumptionReversalIn,
                    SourceType = StockMovementSourceType.Order,
                    SourceId = order.Id,
                    SourceDocumentNo = order.OrderNumber,
                    MovementDate = now,
                    Quantity = req.Value,
                    Note = $"Order line removed, recipe consumption reversed: {order.OrderNumber}"
                },
                cancellationToken);
        }

        orderLine.IsStockDeducted = false;
    }
}
