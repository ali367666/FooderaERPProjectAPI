using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Warehouse.Queries.GetByRestaurantId;

public class GetWarehousesByRestaurantIdQueryHandler
    : IRequestHandler<GetWarehousesByRestaurantIdQuery, BaseResponse<List<WarehouseResponse>>>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetWarehousesByRestaurantIdQueryHandler> _logger;

    public GetWarehousesByRestaurantIdQueryHandler(
        IWarehouseRepository warehouseRepository,
        IMapper mapper,
        ILogger<GetWarehousesByRestaurantIdQueryHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<WarehouseResponse>>> Handle(
        GetWarehousesByRestaurantIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting warehouses by restaurant id: {RestaurantId}", request.RestaurantId);

        var warehouses = await _warehouseRepository.GetByRestaurantIdAsync(request.RestaurantId, cancellationToken);

        var response = _mapper.Map<List<WarehouseResponse>>(warehouses);

        _logger.LogInformation(
            "Warehouses retrieved successfully by restaurant id: {RestaurantId}. Count: {Count}",
            request.RestaurantId,
            response.Count);

        return new BaseResponse<List<WarehouseResponse>>
        {
            Success = true,
            Message = "Warehouses retrieved successfully.",
            Data = response
        };
    }
}