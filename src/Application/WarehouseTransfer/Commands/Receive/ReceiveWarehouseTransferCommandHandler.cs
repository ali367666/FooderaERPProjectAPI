using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using StockBalanceRow = Domain.Entities.WarehouseAndStock.WarehouseStock;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseTransfer.Commands.Receive;

public class ReceiveWarehouseTransferCommandHandler
    : IRequestHandler<ReceiveWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ReceiveWarehouseTransferCommandHandler> _logger;

    public ReceiveWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IWarehouseStockRepository warehouseStockRepository,
        IStockMovementRepository stockMovementRepository,
        IAuditLogService auditLogService,
        ILogger<ReceiveWarehouseTransferCommandHandler> logger)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _warehouseStockRepository = warehouseStockRepository;
        _stockMovementRepository = stockMovementRepository;
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

        foreach (var line in transfer.Lines)
        {
            var vehicleBalance = await _warehouseStockRepository.GetByWarehouseAndStockItemAsync(
                transfer.CompanyId,
                transfer.VehicleWarehouseId.Value,
                line.StockItemId,
                cancellationToken);

            var available = vehicleBalance?.Quantity ?? 0;
            var itemName = line.StockItem.Name;
            if (available < line.Quantity)
            {
                return BaseResponse.Fail(
                    $"Insufficient stock for {itemName}. Available: {available}");
            }
        }

        var oldStatus = transfer.Status;
        var movementDate = DateTime.UtcNow;

        await using var transaction = await _warehouseTransferRepository
            .BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var line in transfer.Lines)
            {
                await ApplyVehicleDecreaseAsync(
                    transfer.CompanyId,
                    transfer.VehicleWarehouseId!.Value,
                    line.StockItemId,
                    line.Quantity,
                    (int)line.StockItem.Unit,
                    cancellationToken);

                var toRow = await _warehouseStockRepository.GetByWarehouseAndStockItemAsync(
                    transfer.CompanyId,
                    transfer.ToWarehouseId,
                    line.StockItemId,
                    cancellationToken);

                if (toRow is null)
                {
                    await _warehouseStockRepository.AddAsync(
                        new StockBalanceRow
                        {
                            CompanyId = transfer.CompanyId,
                            WarehouseId = transfer.ToWarehouseId,
                            StockItemId = line.StockItemId,
                            Quantity = line.Quantity,
                            UnitId = (int)line.StockItem.Unit,
                            CreatedAtUtc = movementDate,
                        },
                        cancellationToken);
                }
                else
                {
                    toRow.Quantity += line.Quantity;
                    toRow.UnitId = (int)line.StockItem.Unit;
                    toRow.LastModifiedAtUtc = movementDate;
                    _warehouseStockRepository.Update(toRow);
                }

                await _stockMovementRepository.AddAsync(
                    new StockMovement
                    {
                        CompanyId = transfer.CompanyId,
                        WarehouseId = transfer.ToWarehouseId,
                        FromWarehouseId = transfer.FromWarehouseId,
                        ToWarehouseId = transfer.ToWarehouseId,
                        StockItemId = line.StockItemId,
                        Type = StockMovementType.TransferIn,
                        SourceType = StockMovementSourceType.WarehouseTransfer,
                        SourceId = transfer.Id,
                        SourceDocumentNo = transfer.DocumentNo,
                        MovementDate = movementDate,
                        Quantity = line.Quantity,
                        WarehouseTransferId = transfer.Id,
                        Note =
                            $"Receive into {transfer.ToWarehouse?.Name ?? "warehouse " + transfer.ToWarehouseId}"
                    },
                    cancellationToken);
            }

            transfer.Status = TransferStatus.Completed;

            _warehouseTransferRepository.Update(transfer);

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

    private async Task ApplyVehicleDecreaseAsync(
        int companyId,
        int warehouseId,
        int stockItemId,
        decimal qty,
        int unitId,
        CancellationToken cancellationToken)
    {
        var row = await _warehouseStockRepository.GetByWarehouseAndStockItemAsync(
            companyId,
            warehouseId,
            stockItemId,
            cancellationToken);

        if (row is null)
            throw new InvalidOperationException("Vehicle warehouse has no balance row for item.");

        row.Quantity -= qty;
        row.UnitId = unitId;
        row.LastModifiedAtUtc = DateTime.UtcNow;

        if (row.Quantity < 0)
            throw new InvalidOperationException("Stock quantity would become negative.");

        _warehouseStockRepository.Update(row);
    }
}
