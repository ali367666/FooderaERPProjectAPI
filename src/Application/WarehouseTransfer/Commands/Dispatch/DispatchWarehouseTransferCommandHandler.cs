using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseTransfer.Commands.Dispatch;

public class DispatchWarehouseTransferCommandHandler
    : IRequestHandler<DispatchWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DispatchWarehouseTransferCommandHandler> _logger;

    public DispatchWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IWarehouseStockRepository warehouseStockRepository,
        IAuditLogService auditLogService,
        ILogger<DispatchWarehouseTransferCommandHandler> logger)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _warehouseStockRepository = warehouseStockRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DispatchWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DispatchWarehouseTransfer başladı. TransferId: {TransferId}",
            request.Id);

        var transfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (transfer is null)
            return BaseResponse.Fail("Warehouse transfer not found.");

        if (transfer.Status != TransferStatus.Approved)
            return BaseResponse.Fail("Only approved transfers can be dispatched.");

        if (!transfer.VehicleWarehouseId.HasValue)
            return BaseResponse.Fail("Vehicle warehouse is required.");

        if (transfer.Lines is null || !transfer.Lines.Any())
            return BaseResponse.Fail("Transfer has no lines.");

        // 🔴 STOCK CHECK
        foreach (var line in transfer.Lines)
        {
            var fromStock = await _warehouseStockRepository
                .GetByWarehouseAndStockItemAsync(
                    transfer.FromWarehouseId,
                    line.StockItemId,
                    cancellationToken);

            if (fromStock is null)
                return BaseResponse.Fail($"Stock item {line.StockItemId} not found in source warehouse.");

            if (fromStock.QuantityOnHand < line.Quantity)
                return BaseResponse.Fail(
                    $"Insufficient stock for item {line.StockItemId}. Available: {fromStock.QuantityOnHand}");
        }

        var oldStatus = transfer.Status;

        await using var transaction = await _warehouseTransferRepository
            .BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var line in transfer.Lines)
            {
                // 🔴 FROM WAREHOUSE ↓
                var fromStock = await _warehouseStockRepository
                    .GetByWarehouseAndStockItemAsync(
                        transfer.FromWarehouseId,
                        line.StockItemId,
                        cancellationToken);

                fromStock!.QuantityOnHand -= line.Quantity;
                _warehouseStockRepository.Update(fromStock);

                // 🔴 VEHICLE WAREHOUSE ↑
                var vehicleStock = await _warehouseStockRepository
                    .GetByWarehouseAndStockItemAsync(
                        transfer.VehicleWarehouseId.Value,
                        line.StockItemId,
                        cancellationToken);

                if (vehicleStock is null)
                {
                    vehicleStock = new Domain.Entities.WarehouseAndStock.WarehouseStock
                    {
                        CompanyId = transfer.CompanyId,
                        WarehouseId = transfer.VehicleWarehouseId.Value,
                        StockItemId = line.StockItemId,
                        QuantityOnHand = line.Quantity
                    };

                    await _warehouseStockRepository.AddAsync(vehicleStock, cancellationToken);
                }
                else
                {
                    vehicleStock.QuantityOnHand += line.Quantity;
                    _warehouseStockRepository.Update(vehicleStock);
                }
            }

            transfer.Status = TransferStatus.InTransit;

            _warehouseTransferRepository.Update(transfer);

            await _warehouseStockRepository.SaveChangesAsync(cancellationToken);
            await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "Dispatch zamanı xəta");

            throw;
        }

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "WarehouseTransfer",
                    EntityId = transfer.Id.ToString(),
                    ActionType = "Dispatch",
                    Message = $"Dispatch edildi. Id: {transfer.Id}, OldStatus: {oldStatus}, NewStatus: {transfer.Status}",
                    IsSuccess = true
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit log error");
        }

        return BaseResponse.Ok("Warehouse transfer dispatched successfully.");
    }
}