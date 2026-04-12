using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockItem.Commands.Create;

public class CreateStockItemCommandHandler
    : IRequestHandler<CreateStockItemCommand, BaseResponse<int>>
{
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IStockCategoryRepository _stockCategoryRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateStockItemCommandHandler> _logger;

    public CreateStockItemCommandHandler(
        IStockItemRepository stockItemRepository,
        IStockCategoryRepository stockCategoryRepository,
        IRestaurantRepository restaurantRepository,
        IMapper mapper,
        ILogger<CreateStockItemCommandHandler> logger)
    {
        _stockItemRepository = stockItemRepository;
        _stockCategoryRepository = stockCategoryRepository;
        _restaurantRepository = restaurantRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<int>> Handle(
        CreateStockItemCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating stock item. Name: {Name}, CompanyId: {CompanyId}, CategoryId: {CategoryId}",
            request.Request.Name,
            request.Request.CompanyId,
            request.Request.CategoryId);

        var categoryExists = await _stockCategoryRepository.ExistsAsync(
            request.Request.CategoryId,
            cancellationToken);

        if (!categoryExists)
        {
            _logger.LogWarning(
                "Stock category not found. CategoryId: {CategoryId}",
                request.Request.CategoryId);

            return BaseResponse<int>.Fail("Stock category not found.");
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

                return BaseResponse<int>.Fail("Restaurant not found.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Request.Barcode))
        {
            var barcodeExists = await _stockItemRepository.BarcodeExistsAsync(
                request.Request.Barcode,
                request.Request.CompanyId,
                cancellationToken);

            if (barcodeExists)
            {
                _logger.LogWarning(
                    "Barcode already exists. Barcode: {Barcode}, CompanyId: {CompanyId}",
                    request.Request.Barcode,
                    request.Request.CompanyId);

                return BaseResponse<int>.Fail("Barcode already exists for this company.");
            }
        }

        var stockItem = _mapper.Map<Domain.Entities.WarehouseAndStock.StockItem>(request.Request);

        await _stockItemRepository.AddAsync(stockItem, cancellationToken);
        await _stockItemRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stock item created successfully. Id: {Id}",
            stockItem.Id);

        return BaseResponse<int>.Ok(stockItem.Id, "Stock item created successfully.");
    }
}