using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockCategory.Commands.Delete;

public class DeleteStockCategoryCommandHandler
    : IRequestHandler<DeleteStockCategoryCommand, BaseResponse>
{
    private readonly IStockCategoryRepository _stockCategoryRepository;
    private readonly ILogger<DeleteStockCategoryCommandHandler> _logger;

    public DeleteStockCategoryCommandHandler(
        IStockCategoryRepository stockCategoryRepository,
        ILogger<DeleteStockCategoryCommandHandler> logger)
    {
        _stockCategoryRepository = stockCategoryRepository;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DeleteStockCategoryCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteStockCategoryCommand started. Id: {Id}",
            request.Id);

        var category = await _stockCategoryRepository.GetByIdAsync(request.Id, cancellationToken);

        if (category is null)
        {
            _logger.LogWarning(
                "DeleteStockCategory failed. Category not found. Id: {Id}",
                request.Id);

            return BaseResponse.Fail("Stock category not found.");
        }

        var hasChildren = await _stockCategoryRepository.HasChildrenAsync(request.Id, cancellationToken);

        if (hasChildren)
        {
            _logger.LogWarning(
                "DeleteStockCategory failed. Category has children. Id: {Id}",
                request.Id);

            return BaseResponse.Fail("Cannot delete category with subcategories.");
        }

        _stockCategoryRepository.Delete(category);
        await _stockCategoryRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stock category deleted successfully. Id: {Id}",
            request.Id);

        return BaseResponse.Ok("Stock category deleted successfully.");
    }
}