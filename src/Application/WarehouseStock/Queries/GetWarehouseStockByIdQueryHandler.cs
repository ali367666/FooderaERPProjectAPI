using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Queries.GetById;

public class GetWarehouseStockByIdQueryHandler
    : IRequestHandler<GetWarehouseStockByIdQuery, BaseResponse<WarehouseStockResponse>>
{
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly ILogger<GetWarehouseStockByIdQueryHandler> _logger;

    public GetWarehouseStockByIdQueryHandler(
        IWarehouseStockRepository warehouseStockRepository,
        ILogger<GetWarehouseStockByIdQueryHandler> logger)
    {
        _warehouseStockRepository = warehouseStockRepository;
        _logger = logger;
    }

    public async Task<BaseResponse<WarehouseStockResponse>> Handle(
        GetWarehouseStockByIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting warehouse stock by id: {Id}", request.Id);

        var warehouseStock = await _warehouseStockRepository.GetByIdAsync(request.Id, cancellationToken);

        if (warehouseStock is null)
        {
            _logger.LogWarning("Warehouse stock not found. Id: {Id}", request.Id);
            return BaseResponse<WarehouseStockResponse>.Fail("Warehouse stock not found.");
        }

        var response = new WarehouseStockResponse
        {
            Id = warehouseStock.Id,
            StockItemId = warehouseStock.StockItemId,
            StockItemName = warehouseStock.StockItem.Name,
            WarehouseId = warehouseStock.WarehouseId,
            WarehouseName = warehouseStock.Warehouse.Name,
            QuantityOnHand = warehouseStock.QuantityOnHand,
            MinLevel = warehouseStock.MinLevel
        };

        return BaseResponse<WarehouseStockResponse>.Ok(response);
    }
}