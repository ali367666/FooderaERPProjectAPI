using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockItem.Commands.Delete;

public class DeleteStockItemCommandHandler
    : IRequestHandler<DeleteStockItemCommand, BaseResponse>
{
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteStockItemCommandHandler> _logger;

    public DeleteStockItemCommandHandler(
        IStockItemRepository stockItemRepository,
        IAuditLogService auditLogService,
        ILogger<DeleteStockItemCommandHandler> logger)
    {
        _stockItemRepository = stockItemRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DeleteStockItemCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting stock item. Id: {Id}", request.Id);

        var stockItem = await _stockItemRepository.GetByIdAsync(request.Id, cancellationToken);

        if (stockItem is null)
        {
            _logger.LogWarning("Stock item not found. Id: {Id}", request.Id);
            return BaseResponse.Fail("Stock item not found.");
        }

        var oldName = stockItem.Name;
        var oldCompanyId = stockItem.CompanyId;
        var oldCategoryId = stockItem.CategoryId;
        var oldBarcode = stockItem.Barcode;

        _stockItemRepository.Delete(stockItem);
        await _stockItemRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "StockItem",
                    EntityId = stockItem.Id.ToString(),
                    ActionType = "Delete",
                    Message = $"StockItem silindi. Id: {stockItem.Id}, Name: {oldName}, CompanyId: {oldCompanyId}, CategoryId: {oldCategoryId}, Barcode: {oldBarcode}",
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
                "StockItem delete audit log yazılarkən xəta baş verdi. StockItemId: {StockItemId}",
                stockItem.Id);
        }

        _logger.LogInformation("Stock item deleted successfully. Id: {Id}", request.Id);

        return BaseResponse.Ok("Stock item deleted successfully.");
    }
}