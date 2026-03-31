using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Queries.GetByWarehouseId;

public class GetWarehouseStocksByWarehouseIdQueryHandler
    : IRequestHandler<GetWarehouseStocksByWarehouseIdQuery, BaseResponse<List<WarehouseStockResponse>>>
{
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly ILogger<GetWarehouseStocksByWarehouseIdQueryHandler> _logger;

    public GetWarehouseStocksByWarehouseIdQueryHandler(
        IWarehouseStockRepository warehouseStockRepository,
        ILogger<GetWarehouseStocksByWarehouseIdQueryHandler> logger)
    {
        _warehouseStockRepository = warehouseStockRepository;
        _logger = logger;
    }

    public async Task<BaseResponse<List<WarehouseStockResponse>>> Handle(
        GetWarehouseStocksByWarehouseIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting warehouse stocks by warehouse id. WarehouseId: {WarehouseId}, Search: {Search}",
            request.WarehouseId,
            request.Search);

        var warehouseStocks = await _warehouseStockRepository.GetByWarehouseIdAsync(
            request.WarehouseId,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();

            warehouseStocks = warehouseStocks
                .Where(x =>
                    x.StockItem.Name.ToLower().Contains(search) ||
                    x.Id.ToString().Contains(search))
                .ToList();
        }

        var response = warehouseStocks.Select(x => new WarehouseStockResponse
        {
            Id = x.Id,
            StockItemId = x.StockItemId,
            StockItemName = x.StockItem.Name,
            WarehouseId = x.WarehouseId,
            WarehouseName = x.Warehouse.Name,
            QuantityOnHand = x.QuantityOnHand,
            MinLevel = x.MinLevel
        }).ToList();

        return BaseResponse<List<WarehouseStockResponse>>.Ok(response);
    }
}