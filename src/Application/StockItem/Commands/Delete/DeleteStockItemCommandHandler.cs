using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockItem.Commands.Delete;

public class DeleteStockItemCommandHandler
    : IRequestHandler<DeleteStockItemCommand, BaseResponse>
{
    private readonly IStockItemRepository _stockItemRepository;
    private readonly ILogger<DeleteStockItemCommandHandler> _logger;

    public DeleteStockItemCommandHandler(
        IStockItemRepository stockItemRepository,
        ILogger<DeleteStockItemCommandHandler> logger)
    {
        _stockItemRepository = stockItemRepository;
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

        _stockItemRepository.Delete(stockItem);
        await _stockItemRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Stock item deleted successfully. Id: {Id}", request.Id);

        return BaseResponse.Ok("Stock item deleted successfully.");
    }
}