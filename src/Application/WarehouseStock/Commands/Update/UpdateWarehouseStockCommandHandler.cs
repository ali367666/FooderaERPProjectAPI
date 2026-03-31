using Application.Common.Helpers;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Commands.Update;

public class UpdateWarehouseStockCommandHandler
    : IRequestHandler<UpdateWarehouseStockCommand, BaseResponse>
{
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly ILogger<UpdateWarehouseStockCommandHandler> _logger;
    private readonly IAuditLogService _auditLogService;

    public UpdateWarehouseStockCommandHandler(
        IWarehouseStockRepository warehouseStockRepository,
        IAuditLogService auditLogService,
        ILogger<UpdateWarehouseStockCommandHandler> logger)
    {
        _warehouseStockRepository = warehouseStockRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        UpdateWarehouseStockCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating warehouse stock. Id: {Id}", request.Id);

        var warehouseStock = await _warehouseStockRepository.GetByIdAsync(request.Id, cancellationToken);

        if (warehouseStock is null)
        {
            _logger.LogWarning("Warehouse stock not found. Id: {Id}", request.Id);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                EntityName = "WarehouseStock",
                EntityId = request.Id.ToString(),
                ActionType = "Update",
                Message = "Warehouse stock update failed. Record not found.",
                IsSuccess = false
            }, cancellationToken);

            return BaseResponse.Fail("Warehouse stock not found.");
        }

        var oldValues = new
        {
            warehouseStock.QuantityOnHand,
            warehouseStock.MinLevel
        };

        if (request.Request.QuantityOnHand.HasValue)
            warehouseStock.QuantityOnHand = request.Request.QuantityOnHand.Value;

        if (request.Request.MinLevel.HasValue)
            warehouseStock.MinLevel = request.Request.MinLevel.Value;

        var newValues = new
        {
            warehouseStock.QuantityOnHand,
            warehouseStock.MinLevel
        };

        await _warehouseStockRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            EntityName = "WarehouseStock",
            EntityId = warehouseStock.Id.ToString(),
            ActionType = "Update",
            OldValues = AuditLogJsonHelper.ToJson(oldValues),
            NewValues = AuditLogJsonHelper.ToJson(newValues),
            Message = "Warehouse stock updated successfully.",
            CompanyId = warehouseStock.CompanyId,
            IsSuccess = true
        }, cancellationToken);

        _logger.LogInformation("Warehouse stock updated successfully. Id: {Id}", request.Id);

        return BaseResponse.Ok("Warehouse stock updated successfully.");
    }
}