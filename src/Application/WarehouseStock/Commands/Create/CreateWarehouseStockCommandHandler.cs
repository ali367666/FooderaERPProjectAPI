using Application.Common.Helpers;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Commands.Create;

public class CreateWarehouseStockCommandHandler
    : IRequestHandler<CreateWarehouseStockCommand, BaseResponse>
{
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreateWarehouseStockCommandHandler> _logger;

    public CreateWarehouseStockCommandHandler(
        IWarehouseStockRepository warehouseStockRepository,
        IWarehouseRepository warehouseRepository,
        IStockItemRepository stockItemRepository,
        IAuditLogService auditLogService,
        ILogger<CreateWarehouseStockCommandHandler> logger)
    {
        _warehouseStockRepository = warehouseStockRepository;
        _warehouseRepository = warehouseRepository;
        _stockItemRepository = stockItemRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        CreateWarehouseStockCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "CreateWarehouseStockCommand started. WarehouseId: {WarehouseId}, StockItemId: {StockItemId}",
            request.Request.WarehouseId,
            request.Request.StockItemId);

        var warehouse = await _warehouseRepository.GetByIdAsync(
            request.Request.WarehouseId,
            cancellationToken);

        if (warehouse is null)
        {
            _logger.LogWarning(
                "CreateWarehouseStockCommand failed. Warehouse not found. WarehouseId: {WarehouseId}",
                request.Request.WarehouseId);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                EntityName = "WarehouseStock",
                EntityId = $"{request.Request.WarehouseId}-{request.Request.StockItemId}",
                ActionType = "Create",
                Message = "Warehouse stock creation failed. Warehouse not found.",
                IsSuccess = false
            }, cancellationToken);

            return BaseResponse.Fail("Warehouse not found.");
        }

        var stockItem = await _stockItemRepository.GetByIdAsync(
            request.Request.StockItemId,
            cancellationToken);

        if (stockItem is null)
        {
            _logger.LogWarning(
                "CreateWarehouseStockCommand failed. StockItem not found. StockItemId: {StockItemId}",
                request.Request.StockItemId);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                EntityName = "WarehouseStock",
                EntityId = $"{request.Request.WarehouseId}-{request.Request.StockItemId}",
                ActionType = "Create",
                Message = "Warehouse stock creation failed. Stock item not found.",
                IsSuccess = false
            }, cancellationToken);

            return BaseResponse.Fail("Stock item not found.");
        }

        if (warehouse.CompanyId != stockItem.CompanyId)
        {
            _logger.LogWarning(
                "CreateWarehouseStockCommand failed. Warehouse and StockItem company mismatch. WarehouseCompanyId: {WarehouseCompanyId}, StockItemCompanyId: {StockItemCompanyId}",
                warehouse.CompanyId,
                stockItem.CompanyId);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                EntityName = "WarehouseStock",
                EntityId = $"{request.Request.WarehouseId}-{request.Request.StockItemId}",
                ActionType = "Create",
                Message = "Warehouse stock creation failed. Warehouse and StockItem do not belong to the same company.",
                CompanyId = warehouse.CompanyId,
                IsSuccess = false
            }, cancellationToken);

            return BaseResponse.Fail("Warehouse and StockItem do not belong to the same company.");
        }

        var existingWarehouseStock =
            await _warehouseStockRepository.GetByWarehouseAndStockItemAsync(
                request.Request.WarehouseId,
                request.Request.StockItemId,
                cancellationToken);

        if (existingWarehouseStock is not null)
        {
            _logger.LogWarning(
                "CreateWarehouseStockCommand failed. WarehouseStock already exists. WarehouseId: {WarehouseId}, StockItemId: {StockItemId}",
                request.Request.WarehouseId,
                request.Request.StockItemId);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                EntityName = "WarehouseStock",
                EntityId = existingWarehouseStock.Id.ToString(),
                ActionType = "Create",
                Message = "Warehouse stock creation failed. This stock item already exists in the selected warehouse.",
                CompanyId = existingWarehouseStock.CompanyId,
                IsSuccess = false
            }, cancellationToken);

            return BaseResponse.Fail("This stock item already exists in the selected warehouse.");
        }

        var warehouseStock = new Domain.Entities.WarehouseAndStock.WarehouseStock
        {
            CompanyId = warehouse.CompanyId,
            WarehouseId = request.Request.WarehouseId,
            StockItemId = request.Request.StockItemId,
            QuantityOnHand = request.Request.QuantityOnHand,
            MinLevel = request.Request.MinLevel
        };

        await _warehouseStockRepository.AddAsync(warehouseStock, cancellationToken);
        await _warehouseStockRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            EntityName = "WarehouseStock",
            EntityId = warehouseStock.Id.ToString(),
            ActionType = "Create",
            NewValues = AuditLogJsonHelper.ToJson(new
            {
                warehouseStock.StockItemId,
                warehouseStock.WarehouseId,
                warehouseStock.QuantityOnHand,
                warehouseStock.MinLevel
            }),
            Message = "Warehouse stock created successfully.",
            CompanyId = warehouseStock.CompanyId,
            IsSuccess = true
        }, cancellationToken);

        _logger.LogInformation(
            "CreateWarehouseStockCommand completed successfully. WarehouseStockId: {WarehouseStockId}",
            warehouseStock.Id);

        return BaseResponse.Ok("Warehouse stock created successfully.");
    }
}