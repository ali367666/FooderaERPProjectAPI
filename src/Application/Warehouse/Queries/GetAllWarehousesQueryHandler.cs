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
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllWarehousesQueryHandler> _logger;

    public GetAllWarehousesQueryHandler(
        IWarehouseRepository warehouseRepository,
        IMapper mapper,
        ILogger<GetAllWarehousesQueryHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<WarehouseResponse>>> Handle(
        GetAllWarehousesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all warehouses.");

        var warehouses = await _warehouseRepository.GetAllAsync(cancellationToken);

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