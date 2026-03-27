using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Warehouse.Queries.GetById;

public class GetWarehouseByIdQueryHandler
    : IRequestHandler<GetWarehouseByIdQuery, BaseResponse<WarehouseResponse>>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetWarehouseByIdQueryHandler> _logger;

    public GetWarehouseByIdQueryHandler(
        IWarehouseRepository warehouseRepository,
        IMapper mapper,
        ILogger<GetWarehouseByIdQueryHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<WarehouseResponse>> Handle(
        GetWarehouseByIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting warehouse by id: {WarehouseId}", request.Id);

        var warehouse = await _warehouseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (warehouse is null)
        {
            _logger.LogWarning("Warehouse not found. WarehouseId: {WarehouseId}", request.Id);

            return new BaseResponse<WarehouseResponse>
            {
                Success = false,
                Message = "Warehouse not found."
            };
        }

        var response = _mapper.Map<WarehouseResponse>(warehouse);

        _logger.LogInformation("Warehouse retrieved successfully. WarehouseId: {WarehouseId}", request.Id);

        return new BaseResponse<WarehouseResponse>
        {
            Success = true,
            Message = "Warehouse retrieved successfully.",
            Data = response
        };
    }
}