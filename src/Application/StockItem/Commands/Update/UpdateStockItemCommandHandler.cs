using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockItem.Commands.Update;

public class UpdateStockItemCommandHandler
    : IRequestHandler<UpdateStockItemCommand, BaseResponse>
{
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IStockCategoryRepository _stockCategoryRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateStockItemCommandHandler> _logger;

    public UpdateStockItemCommandHandler(
        IStockItemRepository stockItemRepository,
        IStockCategoryRepository stockCategoryRepository,
        IRestaurantRepository restaurantRepository,
        IAuditLogService auditLogService,
        ILogger<UpdateStockItemCommandHandler> logger)
    {
        _stockItemRepository = stockItemRepository;
        _stockCategoryRepository = stockCategoryRepository;
        _restaurantRepository = restaurantRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        UpdateStockItemCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating stock item. Id: {Id}", request.Id);

        var stockItem = await _stockItemRepository.GetByIdAsync(request.Id, cancellationToken);

        if (stockItem is null)
        {
            _logger.LogWarning("Stock item not found. Id: {Id}", request.Id);
            return BaseResponse.Fail("Stock item not found.");
        }

        var categoryExists = await _stockCategoryRepository.ExistsAsync(
            request.Request.CategoryId,
            cancellationToken);

        if (!categoryExists)
        {
            _logger.LogWarning(
                "Stock category not found. CategoryId: {CategoryId}",
                request.Request.CategoryId);

            return BaseResponse.Fail("Stock category not found.");
        }

        if (request.Request.RestaurantId.HasValue)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(
                request.Request.RestaurantId.Value,
                cancellationToken);

            if (restaurant is null)
            {
                _logger.LogWarning(
                    "Restaurant not found. RestaurantId: {RestaurantId}",
                    request.Request.RestaurantId.Value);

                return BaseResponse.Fail("Restaurant not found.");
            }
        }

        var oldName = stockItem.Name;
        var oldBarcode = stockItem.Barcode;
        var oldType = stockItem.Type;
        var oldUnit = stockItem.Unit;
        var oldCategoryId = stockItem.CategoryId;
        var oldCompanyId = stockItem.CompanyId;
        

        stockItem.Name = request.Request.Name;
        stockItem.Barcode = request.Request.Barcode;
        stockItem.Type = request.Request.Type;
        stockItem.Unit = request.Request.Unit;
        stockItem.CategoryId = request.Request.CategoryId;
        stockItem.CompanyId = request.Request.CompanyId;
        // stockItem.RestaurantId = request.Request.RestaurantId;

        _stockItemRepository.Update(stockItem);
        await _stockItemRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "StockItem",
                    EntityId = stockItem.Id.ToString(),
                    ActionType = "Update",
                    Message = $"StockItem yeniləndi. Id: {stockItem.Id}, OldName: {oldName}, NewName: {stockItem.Name}, OldBarcode: {oldBarcode}, NewBarcode: {stockItem.Barcode}, OldType: {oldType}, NewType: {stockItem.Type}, OldUnit: {oldUnit}, NewUnit: {stockItem.Unit}, OldCategoryId: {oldCategoryId}, NewCategoryId: {stockItem.CategoryId}, OldCompanyId: {oldCompanyId}, NewCompanyId: {stockItem.CompanyId}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "StockItem üçün audit log yazıldı. StockItemId: {StockItemId}",
                stockItem.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "StockItem update audit log yazılarkən xəta baş verdi. StockItemId: {StockItemId}",
                stockItem.Id);
        }

        _logger.LogInformation("Stock item updated successfully. Id: {Id}", stockItem.Id);

        return BaseResponse.Ok("Stock item updated successfully.");
    }
}