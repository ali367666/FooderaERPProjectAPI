using Application.Common.Interfaces;
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
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchWarehousesQueryHandler> _logger;

    public SearchWarehousesQueryHandler(
        IWarehouseRepository warehouseRepository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<SearchWarehousesQueryHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<WarehouseResponse>>> Handle(
        SearchWarehousesQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.IsSuperAdmin ? request.CompanyId : _currentUserService.CompanyId;

        _logger.LogInformation(
            "Searching warehouses. CompanyId: {CompanyId}, Search: {Search}",
            companyId,
            request.Search);

        var warehouses = await _warehouseRepository.SearchAsync(
            companyId,
            request.Search,
            cancellationToken);

        var response = _mapper.Map<List<WarehouseResponse>>(warehouses);

        return BaseResponse<List<WarehouseResponse>>.Ok(
            response,
            "Warehouses retrieved successfully.");
    }
}