using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Warehouse.Queries.GetByCompanyId;

public class GetWarehousesByCompanyIdQueryHandler
    : IRequestHandler<GetWarehousesByCompanyIdQuery, BaseResponse<List<WarehouseResponse>>>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetWarehousesByCompanyIdQueryHandler> _logger;

    public GetWarehousesByCompanyIdQueryHandler(
        IWarehouseRepository warehouseRepository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<GetWarehousesByCompanyIdQueryHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<WarehouseResponse>>> Handle(
        GetWarehousesByCompanyIdQuery request,
        CancellationToken cancellationToken)
    {
        // A tenant Admin is always confined to their own company, regardless of what companyId was
        // requested in the route — only SuperAdmin may target an arbitrary company.
        var companyId = _currentUserService.IsSuperAdmin ? request.CompanyId : _currentUserService.CompanyId;

        _logger.LogInformation("Getting warehouses by company id: {CompanyId}", companyId);

        var warehouses = await _warehouseRepository.GetByCompanyIdAsync(companyId, cancellationToken);

        var response = _mapper.Map<List<WarehouseResponse>>(warehouses);

        _logger.LogInformation(
            "Warehouses retrieved successfully by company id: {CompanyId}. Count: {Count}",
            request.CompanyId,
            response.Count);

        return new BaseResponse<List<WarehouseResponse>>
        {
            Success = true,
            Message = "Warehouses retrieved successfully.",
            Data = response
        };
    }
}