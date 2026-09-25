using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Warehouse.Queries.GetAll;

public class GetAllWarehousesQueryHandler
    : IRequestHandler<GetAllWarehousesQuery, BaseResponse<List<WarehouseResponse>>>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllWarehousesQueryHandler> _logger;

    public GetAllWarehousesQueryHandler(
        IWarehouseRepository warehouseRepository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<GetAllWarehousesQueryHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<WarehouseResponse>>> Handle(
        GetAllWarehousesQuery request,
        CancellationToken cancellationToken)
    {
        // A tenant Admin only ever sees their own company's warehouses. SuperAdmin can target one
        // company via CompanyId, or omit it to browse every company (cross-tenant "All Companies" view).
        List<Domain.Entities.Warehouse> warehouses;
        if (!_currentUserService.IsSuperAdmin)
        {
            _logger.LogInformation("Getting warehouses for own company: {CompanyId}", _currentUserService.CompanyId);
            warehouses = await _warehouseRepository.GetByCompanyIdAsync(_currentUserService.CompanyId, cancellationToken);
        }
        else if (request.CompanyId is > 0)
        {
            _logger.LogInformation("SuperAdmin getting warehouses for company: {CompanyId}", request.CompanyId);
            warehouses = await _warehouseRepository.GetByCompanyIdAsync(request.CompanyId.Value, cancellationToken);
        }
        else
        {
            _logger.LogInformation("SuperAdmin getting all warehouses across every company.");
            warehouses = await _warehouseRepository.GetAllAsync(cancellationToken);
        }

        var response = _mapper.Map<List<WarehouseResponse>>(warehouses);

        _logger.LogInformation("All warehouses retrieved successfully. Count: {Count}", response.Count);

        return new BaseResponse<List<WarehouseResponse>>
        {
            Success = true,
            Message = "Warehouses retrieved successfully.",
            Data = response
        };
    }
}