using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockItem.Commands.Update;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockItem.Commands.Patch;

public class PatchStockItemCommandHandler
    : IRequestHandler<PatchStockItemCommand, BaseResponse>
{
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IStockCategoryRepository _stockCategoryRepository;
    private readonly ILogger<PatchStockItemCommandHandler> _logger;

    public PatchStockItemCommandHandler(
        IStockItemRepository stockItemRepository,
        IStockCategoryRepository stockCategoryRepository,
        ILogger<PatchStockItemCommandHandler> logger)
    {
        _stockItemRepository = stockItemRepository;
        _stockCategoryRepository = stockCategoryRepository;
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

        if (!string.IsNullOrWhiteSpace(request.Request.Name))
            stockItem.Name = request.Request.Name;

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

        _logger.LogInformation("Stock item patched successfully. Id: {Id}", stockItem.Id);

        return BaseResponse.Ok("Stock item patched successfully.");
    }
}