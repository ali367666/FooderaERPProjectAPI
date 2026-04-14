using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Warehouse.Commands.Patch;

public class PatchWarehouseCommandHandler
    : IRequestHandler<PatchWarehouseCommand, BaseResponse>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PatchWarehouseCommandHandler> _logger;

    public PatchWarehouseCommandHandler(
        IWarehouseRepository warehouseRepository,
        IRestaurantRepository restaurantRepository,
        IAuditLogService auditLogService,
        ILogger<PatchWarehouseCommandHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _restaurantRepository = restaurantRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        PatchWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Patching warehouse. WarehouseId: {WarehouseId}",
            request.Id);

        var warehouse = await _warehouseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (warehouse is null)
        {
            _logger.LogWarning(
                "PatchWarehouse failed. Warehouse not found. WarehouseId: {WarehouseId}",
                request.Id);

            return BaseResponse.Fail("Warehouse not found.");
        }

        var oldName = warehouse.Name;
        var oldType = warehouse.Type;
        var oldRestaurantId = warehouse.RestaurantId;
        var oldDriverUserId = warehouse.DriverUserId;

        if (!string.IsNullOrWhiteSpace(request.Request.Name))
            warehouse.Name = request.Request.Name.Trim();

        if (request.Request.Type.HasValue)
            warehouse.Type = request.Request.Type.Value;

        if (request.Request.RestaurantId.HasValue)
        {
            if (request.Request.RestaurantId.Value == 0)
            {
                warehouse.RestaurantId = null;
            }
            else
            {
                var restaurant = await _restaurantRepository
                    .GetByIdAsync(request.Request.RestaurantId.Value, cancellationToken);

                if (restaurant is null)
                {
                    _logger.LogWarning(
                        "PatchWarehouse failed. Restaurant not found. RestaurantId: {RestaurantId}",
                        request.Request.RestaurantId.Value);

                    return BaseResponse.Fail("Restaurant not found.");
                }

                if (restaurant.CompanyId != warehouse.CompanyId)
                {
                    _logger.LogWarning(
                        "PatchWarehouse failed. Restaurant does not belong to same company. WarehouseId: {WarehouseId}, RestaurantId: {RestaurantId}, CompanyId: {CompanyId}",
                        warehouse.Id,
                        restaurant.Id,
                        warehouse.CompanyId);

                    return BaseResponse.Fail("Restaurant does not belong to this company.");
                }

                warehouse.RestaurantId = request.Request.RestaurantId.Value;
            }
        }

        if (request.Request.DriverUserId.HasValue)
        {
            if (request.Request.DriverUserId.Value == 0)
            {
                warehouse.DriverUserId = null;
            }
            else
            {
                warehouse.DriverUserId = request.Request.DriverUserId.Value;
            }
        }

        await _warehouseRepository.UpdateAsync(warehouse, cancellationToken);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Warehouse",
                    EntityId = warehouse.Id.ToString(),
                    ActionType = "Patch",
                    Message = $"Warehouse patch olundu. Id: {warehouse.Id}, OldName: {oldName}, NewName: {warehouse.Name}, OldType: {oldType}, NewType: {warehouse.Type}, OldRestaurantId: {oldRestaurantId}, NewRestaurantId: {warehouse.RestaurantId}, OldDriverUserId: {oldDriverUserId}, NewDriverUserId: {warehouse.DriverUserId}",
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
                "Warehouse patch audit log yazılarkən xəta baş verdi. WarehouseId: {WarehouseId}",
                warehouse.Id);
        }

        _logger.LogInformation(
            "Warehouse patched successfully. WarehouseId: {WarehouseId}",
            warehouse.Id);

        return BaseResponse.Ok("Warehouse updated successfully.");
    }
}