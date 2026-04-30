using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Queries.SearchBalances;

public class SearchWarehouseStockBalancesQueryHandler
    : IRequestHandler<SearchWarehouseStockBalancesQuery, BaseResponse<List<WarehouseStockBalanceResponse>>>
{
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly ILogger<SearchWarehouseStockBalancesQueryHandler> _logger;

    public SearchWarehouseStockBalancesQueryHandler(
        IWarehouseStockRepository warehouseStockRepository,
        ILogger<SearchWarehouseStockBalancesQueryHandler> logger)
    {
        _warehouseStockRepository = warehouseStockRepository;
        _logger = logger;
    }

    public async Task<BaseResponse<List<WarehouseStockBalanceResponse>>> Handle(
        SearchWarehouseStockBalancesQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _warehouseStockRepository.SearchAsync(
            request.CompanyId,
            request.WarehouseId,
            request.StockItemId,
            request.Search,
            cancellationToken);

        var response = rows.Select(x => new WarehouseStockBalanceResponse
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            WarehouseId = x.WarehouseId,
            WarehouseName = x.Warehouse.Name,
            StockItemId = x.StockItemId,
            StockItemName = x.StockItem.Name,
            Quantity = x.Quantity,
            UnitId = x.UnitId
        }).ToList();

        _logger.LogInformation(
            "SearchWarehouseStockBalances: company {CompanyId}, count {Count}",
            request.CompanyId,
            response.Count);

        return BaseResponse<List<WarehouseStockBalanceResponse>>.Ok(response);
    }
}
