using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Warehouse.Commands.Delete;

public class DeleteWarehouseCommandHandler : IRequestHandler<DeleteWarehouseCommand, BaseResponse>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteWarehouseCommandHandler> _logger;

    public DeleteWarehouseCommandHandler(
        IWarehouseRepository warehouseRepository,
        IAuditLogService auditLogService,
        ILogger<DeleteWarehouseCommandHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DeleteWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteWarehouseCommand started. WarehouseId: {WarehouseId}",
            request.Id);

        var warehouse = await _warehouseRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (warehouse is null)
        {
            _logger.LogWarning(
                "Warehouse not found. WarehouseId: {WarehouseId}",
                request.Id);

            return new BaseResponse
            {
                Success = false,
                Message = "Warehouse not found."
            };
        }

        // 🔥 OLD VALUES (audit üçün)
        var oldName = warehouse.Name;
        var oldType = warehouse.Type;
        var oldCompanyId = warehouse.CompanyId;
        var oldRestaurantId = warehouse.RestaurantId;
        var oldDriverUserId = warehouse.DriverUserId;

        _warehouseRepository.Delete(warehouse);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Warehouse",
                    EntityId = warehouse.Id.ToString(),
                    ActionType = "Delete",
                    Message = $"Warehouse silindi. Id: {warehouse.Id}, Name: {oldName}, Type: {oldType}, CompanyId: {oldCompanyId}, RestaurantId: {oldRestaurantId}, DriverUserId: {oldDriverUserId}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "Warehouse üçün audit log yazıldı. WarehouseId: {WarehouseId}",
                warehouse.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "Warehouse delete audit log yazılarkən xəta baş verdi. WarehouseId: {WarehouseId}",
                warehouse.Id);
        }

        _logger.LogInformation(
            "Warehouse deleted successfully. WarehouseId: {WarehouseId}",
            request.Id);

        return new BaseResponse
        {
            Success = true,
            Message = "Warehouse deleted successfully."
        };
    }
}