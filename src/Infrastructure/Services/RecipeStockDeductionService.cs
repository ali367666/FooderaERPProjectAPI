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

    public async Task DeductForOrderLineAsync(OrderLine orderLine, CancellationToken cancellationToken)
    {
        if (orderLine.IsStockDeducted)
            return;

        var companyId = orderLine.CompanyId;
        var order = await _orderRepository.GetByIdAsync(orderLine.OrderId, companyId, cancellationToken);
        if (order is null)
            throw new NotFoundException("Order not found.");

        var recipeLines = await _menuItemRecipeRepository.GetByMenuItemIdAsync(companyId, orderLine.MenuItemId, cancellationToken);
        if (recipeLines.Count == 0)
            return;

        var restaurantWarehouse = (await _warehouseRepository.GetByRestaurantIdAsync(order.RestaurantId, cancellationToken))
            .FirstOrDefault(x => x.CompanyId == companyId && x.Type == WarehouseType.Restaurant);
        restaurantWarehouse ??= (await _warehouseRepository.GetByRestaurantIdAsync(order.RestaurantId, cancellationToken))
            .FirstOrDefault(x => x.CompanyId == companyId);
        if (restaurantWarehouse is null)
            throw new BadRequestException("Restaurant warehouse was not found.");

        var requiredByStockItem = new Dictionary<int, decimal>();
        foreach (var recipeLine in recipeLines)
        {
            var requiredQuantity = recipeLine.QuantityPerPortion * orderLine.Quantity;
            if (!requiredByStockItem.TryAdd(recipeLine.StockItemId, requiredQuantity))
                requiredByStockItem[recipeLine.StockItemId] += requiredQuantity;
        }

        foreach (var req in requiredByStockItem)
        {
            var balance = await _context.WarehouseStocks.FirstOrDefaultAsync(
                x => x.CompanyId == companyId
                    && x.WarehouseId == restaurantWarehouse.Id
                    && x.StockItemId == req.Key,
                cancellationToken);
            if (balance is null)
            {
                var itemName = recipeLines.FirstOrDefault(x => x.StockItemId == req.Key)?.StockItem.Name ?? $"StockItem#{req.Key}";
                throw new BadRequestException($"Anbarda stok tapılmadı: {itemName}");
            }
            if (balance.Quantity < req.Value)
            {
                var itemName = recipeLines.FirstOrDefault(x => x.StockItemId == req.Key)?.StockItem.Name ?? $"StockItem#{req.Key}";
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

            Console.WriteLine("DEDUCTION CALLED");
            Console.WriteLine($"WarehouseStock before: {balance.Quantity}");
            Console.WriteLine($"Required: {req.Value}");
            Console.WriteLine($"WarehouseStock after: {balance.Quantity - req.Value}");
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
}
