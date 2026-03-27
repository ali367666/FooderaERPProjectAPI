using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Warehouse.Commands.Delete;

public class DeleteWarehouseCommandHandler : IRequestHandler<DeleteWarehouseCommand, BaseResponse>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ILogger<DeleteWarehouseCommandHandler> _logger;

    public DeleteWarehouseCommandHandler(
        IWarehouseRepository warehouseRepository,
        ILogger<DeleteWarehouseCommandHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(DeleteWarehouseCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteWarehouseCommand started. WarehouseId: {WarehouseId}", request.Id);

        var warehouse = await _warehouseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (warehouse is null)
        {
            _logger.LogWarning("Warehouse not found. WarehouseId: {WarehouseId}", request.Id);

            return new BaseResponse
            {
                Success = false,
                Message = "Warehouse not found."
            };
        }

        _warehouseRepository.Delete(warehouse);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Warehouse deleted successfully. WarehouseId: {WarehouseId}", request.Id);

        return new BaseResponse
        {
            Success = true,
            Message = "Warehouse deleted successfully."
        };
    }
}