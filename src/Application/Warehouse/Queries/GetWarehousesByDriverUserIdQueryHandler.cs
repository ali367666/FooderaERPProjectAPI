using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Warehouse.Queries.GetByDriverUserId;

public class GetWarehousesByDriverUserIdQueryHandler
    : IRequestHandler<GetWarehousesByDriverUserIdQuery, BaseResponse<List<WarehouseResponse>>>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetWarehousesByDriverUserIdQueryHandler> _logger;

    public GetWarehousesByDriverUserIdQueryHandler(
        IWarehouseRepository warehouseRepository,
        IMapper mapper,
        ILogger<GetWarehousesByDriverUserIdQueryHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<WarehouseResponse>>> Handle(
        GetWarehousesByDriverUserIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting warehouses by driver user id: {DriverUserId}", request.DriverUserId);

        var warehouses = await _warehouseRepository.GetByDriverUserIdAsync(request.DriverUserId, cancellationToken);

        var response = _mapper.Map<List<WarehouseResponse>>(warehouses);

        _logger.LogInformation(
            "Warehouses retrieved successfully by driver user id: {DriverUserId}. Count: {Count}",
            request.DriverUserId,
            response.Count);

        return new BaseResponse<List<WarehouseResponse>>
        {
            Success = true,
            Message = "Warehouses retrieved successfully.",
            Data = response
        };
    }
}