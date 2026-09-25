using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using Domain.Enums;
using MediatR;

namespace Application.WarehouseStock.Queries.GetPosBalances;

public class GetPosWarehouseStockBalancesQueryHandler
    : IRequestHandler<GetPosWarehouseStockBalancesQuery, BaseResponse<List<WarehouseStockBalanceResponse>>>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetPosWarehouseStockBalancesQueryHandler(
        IWarehouseRepository warehouseRepository,
        IWarehouseStockRepository warehouseStockRepository,
        ICurrentUserService currentUserService)
    {
        _warehouseRepository = warehouseRepository;
        _warehouseStockRepository = warehouseStockRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<List<WarehouseStockBalanceResponse>>> Handle(
        GetPosWarehouseStockBalancesQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var warehouses = await _warehouseRepository.GetByRestaurantIdAsync(request.RestaurantId, cancellationToken);
        var restaurantWarehouse = warehouses.FirstOrDefault(x => x.CompanyId == companyId && x.Type == WarehouseType.Restaurant)
            ?? warehouses.FirstOrDefault(x => x.CompanyId == companyId);

        if (restaurantWarehouse is null)
            throw new BadRequestException("Filial anbarı tapılmadı.");

        var rows = await _warehouseStockRepository.SearchAsync(
            companyId,
            restaurantWarehouse.Id,
            null,
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

        return BaseResponse<List<WarehouseStockBalanceResponse>>.Ok(response);
    }
}
