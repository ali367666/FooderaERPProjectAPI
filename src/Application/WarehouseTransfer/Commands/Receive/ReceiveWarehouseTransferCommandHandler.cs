using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseTransfer.Commands.Receive;

public class ReceiveWarehouseTransferCommandHandler
    : IRequestHandler<ReceiveWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ReceiveWarehouseTransferCommandHandler> _logger;

    public ReceiveWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IWarehouseStockRepository warehouseStockRepository,
        IAuditLogService auditLogService,
        ILogger<ReceiveWarehouseTransferCommandHandler> logger)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _warehouseStockRepository = warehouseStockRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        ReceiveWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ReceiveWarehouseTransfer başladı. TransferId: {TransferId}",
            request.Id);

        var transfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (transfer is null)
            return BaseResponse.Fail("Warehouse transfer not found.");

        if (transfer.Status != TransferStatus.InTransit)
            return BaseResponse.Fail("Only in-transit transfers can be received.");

        if (!transfer.VehicleWarehouseId.HasValue)
            return BaseResponse.Fail("Vehicle warehouse is required.");

        if (transfer.Lines is null || !transfer.Lines.Any())
            return BaseResponse.Fail("Transfer has no lines.");

        var oldStatus = transfer.Status;

        await using var transaction = await _warehouseTransferRepository
            .BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var line in transfer.Lines)
            {
                // 🔴 VEHICLE WAREHOUSE ↓
                var vehicleStock = await _warehouseStockRepository
                    .GetByWarehouseAndStockItemAsync(
                        transfer.VehicleWarehouseId.Value,
                        line.StockItemId,
                        cancellationToken);

                if (vehicleStock is null || vehicleStock.QuantityOnHand < line.Quantity)
                {
                    return BaseResponse.Fail(
                        $"Vehicle warehouse stock not enough for item {line.StockItemId}");
                }

                vehicleStock.QuantityOnHand -= line.Quantity;
                _warehouseStockRepository.Update(vehicleStock);

                // 🔴 TO WAREHOUSE ↑
                var toStock = await _warehouseStockRepository
                    .GetByWarehouseAndStockItemAsync(
                        transfer.ToWarehouseId,
                        line.StockItemId,
                        cancellationToken);

                if (toStock is null)
                {
                    var newStock = new Domain.Entities.WarehouseAndStock.WarehouseStock
                    {
                        CompanyId = transfer.CompanyId,
                        WarehouseId = transfer.ToWarehouseId,
                        StockItemId = line.StockItemId,
                        QuantityOnHand = line.Quantity
                    };

                    await _warehouseStockRepository.AddAsync(newStock, cancellationToken);
                }
                else
                {
                    toStock.QuantityOnHand += line.Quantity;
                    _warehouseStockRepository.Update(toStock);
                }
            }

            transfer.Status = TransferStatus.Completed;

            _warehouseTransferRepository.Update(transfer);

            await _warehouseStockRepository.SaveChangesAsync(cancellationToken);
            await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "Receive zamanı xəta");

            throw;
        }

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "WarehouseTransfer",
                    EntityId = transfer.Id.ToString(),
                    ActionType = "Receive",
                    Message = $"Receive edildi. Id: {transfer.Id}, OldStatus: {oldStatus}, NewStatus: {transfer.Status}",
                    IsSuccess = true
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit log error");
        }

        return BaseResponse.Ok("Warehouse transfer received successfully.");
    }
}