using Application.Common.Helpers;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Commands.Delete;

public class DeleteWarehouseStockCommandHandler
    : IRequestHandler<DeleteWarehouseStockCommand, BaseResponse>
{
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteWarehouseStockCommandHandler> _logger;

    public DeleteWarehouseStockCommandHandler(
        IWarehouseStockRepository warehouseStockRepository,
        IAuditLogService auditLogService,
        ILogger<DeleteWarehouseStockCommandHandler> logger)
    {
        _warehouseStockRepository = warehouseStockRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DeleteWarehouseStockCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting warehouse stock. Id: {Id}", request.Id);

        var warehouseStock = await _warehouseStockRepository.GetByIdAsync(request.Id, cancellationToken);

        if (warehouseStock is null)
        {
            _logger.LogWarning("Warehouse stock not found. Id: {Id}", request.Id);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                EntityName = "WarehouseStock",
                EntityId = request.Id.ToString(),
                ActionType = "Delete",
                Message = "Warehouse stock delete failed. Record not found.",
                IsSuccess = false
            }, cancellationToken);

            return BaseResponse.Fail("Warehouse stock not found.");
        }

        var oldValues = new
        {
            warehouseStock.StockItemId,
            warehouseStock.WarehouseId,
            warehouseStock.QuantityOnHand,
            warehouseStock.MinLevel
        };

        _warehouseStockRepository.Delete(warehouseStock);
        await _warehouseStockRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            EntityName = "WarehouseStock",
            EntityId = warehouseStock.Id.ToString(),
            ActionType = "Delete",
            OldValues = AuditLogJsonHelper.ToJson(oldValues),
            Message = "Warehouse stock deleted successfully.",
            CompanyId = warehouseStock.CompanyId,
            IsSuccess = true
        }, cancellationToken);

        _logger.LogInformation("Warehouse stock deleted successfully. Id: {Id}", request.Id);

        return BaseResponse.Ok("Warehouse stock deleted successfully.");
    }
}