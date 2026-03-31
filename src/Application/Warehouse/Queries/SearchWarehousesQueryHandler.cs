using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Warehouse.Queries.Search;

public class SearchWarehousesQueryHandler
    : IRequestHandler<SearchWarehousesQuery, BaseResponse<List<WarehouseResponse>>>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchWarehousesQueryHandler> _logger;

    public SearchWarehousesQueryHandler(
        IWarehouseRepository warehouseRepository,
        IMapper mapper,
        ILogger<SearchWarehousesQueryHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<WarehouseResponse>>> Handle(
        SearchWarehousesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Searching warehouses. CompanyId: {CompanyId}, Search: {Search}",
            request.CompanyId,
            request.Search);

        var warehouses = await _warehouseRepository.SearchAsync(
            request.CompanyId,
            request.Search,
            cancellationToken);

        var response = _mapper.Map<List<WarehouseResponse>>(warehouses);

        return BaseResponse<List<WarehouseResponse>>.Ok(
            response,
            "Warehouses retrieved successfully.");
    }
}