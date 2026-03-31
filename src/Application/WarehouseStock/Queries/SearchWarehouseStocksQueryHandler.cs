using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Queries.Search;

public class SearchWarehouseStocksQueryHandler
    : IRequestHandler<SearchWarehouseStocksQuery, BaseResponse<List<WarehouseStockResponse>>>
{
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly ILogger<SearchWarehouseStocksQueryHandler> _logger;

    public SearchWarehouseStocksQueryHandler(
        IWarehouseStockRepository warehouseStockRepository,
        ILogger<SearchWarehouseStocksQueryHandler> logger)
    {
        _warehouseStockRepository = warehouseStockRepository;
        _logger = logger;
    }

    public async Task<BaseResponse<List<WarehouseStockResponse>>> Handle(
        SearchWarehouseStocksQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Searching warehouse stocks. CompanyId: {CompanyId}, Search: {Search}",
            request.CompanyId,
            request.Search);

        var warehouseStocks = await _warehouseStockRepository.SearchAsync(
            request.CompanyId,
            request.Search,
            cancellationToken);

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