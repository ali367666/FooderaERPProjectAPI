using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockItem.Commands.Patch;

public class PatchStockItemCommandHandler
    : IRequestHandler<PatchStockItemCommand, BaseResponse>
{
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IStockCategoryRepository _stockCategoryRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PatchStockItemCommandHandler> _logger;

    public PatchStockItemCommandHandler(
        IStockItemRepository stockItemRepository,
        IStockCategoryRepository stockCategoryRepository,
        IAuditLogService auditLogService,
        ILogger<PatchStockItemCommandHandler> logger)
    {
        _stockItemRepository = stockItemRepository;
        _stockCategoryRepository = stockCategoryRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        PatchStockItemCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Patching stock item. Id: {Id}", request.Id);

        var stockItem = await _stockItemRepository.GetByIdAsync(request.Id, cancellationToken);

        if (stockItem is null)
        {
            _logger.LogWarning("Stock item not found. Id: {Id}", request.Id);
            return BaseResponse.Fail("Stock item not found.");
        }

        if (request.Request.CategoryId.HasValue)
        {
            var categoryExists = await _stockCategoryRepository.ExistsAsync(
                request.Request.CategoryId.Value,
                cancellationToken);

            if (!categoryExists)
            {
                _logger.LogWarning(
                    "Stock category not found. CategoryId: {CategoryId}",
                    request.Request.CategoryId.Value);

                return BaseResponse.Fail("Stock category not found.");
            }
        }

        var oldName = stockItem.Name;
        var oldBarcode = stockItem.Barcode;
        var oldType = stockItem.Type;
        var oldUnit = stockItem.Unit;
        var oldCategoryId = stockItem.CategoryId;

        if (!string.IsNullOrWhiteSpace(request.Request.Name))
            stockItem.Name = request.Request.Name.Trim();

        if (request.Request.Barcode is not null)
            stockItem.Barcode = request.Request.Barcode;

        if (request.Request.Type.HasValue)
            stockItem.Type = request.Request.Type.Value;

        if (request.Request.Unit.HasValue)
            stockItem.Unit = request.Request.Unit.Value;

        if (request.Request.CategoryId.HasValue)
            stockItem.CategoryId = request.Request.CategoryId.Value;

        _stockItemRepository.Update(stockItem);
        await _stockItemRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "StockItem",
                    EntityId = stockItem.Id.ToString(),
                    ActionType = "Patch",
                    Message = $"StockItem patch olundu. Id: {stockItem.Id}, OldName: {oldName}, NewName: {stockItem.Name}, OldBarcode: {oldBarcode}, NewBarcode: {stockItem.Barcode}, OldType: {oldType}, NewType: {stockItem.Type}, OldUnit: {oldUnit}, NewUnit: {stockItem.Unit}, OldCategoryId: {oldCategoryId}, NewCategoryId: {stockItem.CategoryId}",
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
                "StockItem patch audit log yazılarkən xəta baş verdi. StockItemId: {StockItemId}",
                stockItem.Id);
        }

        _logger.LogInformation("Stock item patched successfully. Id: {Id}", stockItem.Id);

        return BaseResponse.Ok("Stock item patched successfully.");
    }
}