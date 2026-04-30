using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using StockBalanceRow = Domain.Entities.WarehouseAndStock.WarehouseStock;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseTransfer.Commands.Dispatch;

public class DispatchWarehouseTransferCommandHandler
    : IRequestHandler<DispatchWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DispatchWarehouseTransferCommandHandler> _logger;

    public DispatchWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IWarehouseStockRepository warehouseStockRepository,
        IStockMovementRepository stockMovementRepository,
        IAuditLogService auditLogService,
        ILogger<DispatchWarehouseTransferCommandHandler> logger)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _warehouseStockRepository = warehouseStockRepository;
        _stockMovementRepository = stockMovementRepository;
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

        foreach (var line in transfer.Lines)
        {
            var fromBalance = await _warehouseStockRepository.GetByWarehouseAndStockItemAsync(
                transfer.CompanyId,
                transfer.FromWarehouseId,
                line.StockItemId,
                cancellationToken);

            var available = fromBalance?.Quantity ?? 0;
            var itemName = line.StockItem.Name;
            if (available < line.Quantity)
                return BaseResponse.Fail(
                    $"Insufficient stock for {itemName}. Available: {available}");
        }

        var oldStatus = transfer.Status;
        var movementDate = DateTime.UtcNow;

        await using var transaction = await _warehouseTransferRepository
            .BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var line in transfer.Lines)
            {
                await ApplyQuantityDeltaAsync(
                    transfer.CompanyId,
                    transfer.FromWarehouseId,
                    line.StockItemId,
                    -line.Quantity,
                    (int)line.StockItem.Unit,
                    cancellationToken);

                await ApplyQuantityDeltaAsync(
                    transfer.CompanyId,
                    transfer.VehicleWarehouseId!.Value,
                    line.StockItemId,
                    line.Quantity,
                    (int)line.StockItem.Unit,
                    cancellationToken);

                await _stockMovementRepository.AddAsync(
                    new StockMovement
                    {
                        CompanyId = transfer.CompanyId,
                        WarehouseId = transfer.FromWarehouseId,
                        FromWarehouseId = transfer.FromWarehouseId,
                        ToWarehouseId = transfer.ToWarehouseId,
                        StockItemId = line.StockItemId,
                        Type = StockMovementType.TransferOut,
                        SourceType = StockMovementSourceType.WarehouseTransfer,
                        SourceId = transfer.Id,
                        SourceDocumentNo = transfer.DocumentNo,
                        MovementDate = movementDate,
                        Quantity = line.Quantity,
                        WarehouseTransferId = transfer.Id,
                        Note =
                            $"Dispatch from {transfer.FromWarehouse?.Name ?? "warehouse " + transfer.FromWarehouseId} toward {transfer.ToWarehouse?.Name ?? "warehouse " + transfer.ToWarehouseId}"
                    },
                    cancellationToken);
            }

            transfer.Status = TransferStatus.InTransit;

            _warehouseTransferRepository.Update(transfer);

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

    private async Task ApplyQuantityDeltaAsync(
        int companyId,
        int warehouseId,
        int stockItemId,
        decimal delta,
        int unitId,
        CancellationToken cancellationToken)
    {
        var row = await _warehouseStockRepository.GetByWarehouseAndStockItemAsync(
            companyId,
            warehouseId,
            stockItemId,
            cancellationToken);

        if (row is null)
        {
            if (delta >= 0)
            {
                await _warehouseStockRepository.AddAsync(
                    new StockBalanceRow
                    {
                        CompanyId = companyId,
                        WarehouseId = warehouseId,
                        StockItemId = stockItemId,
                        Quantity = delta,
                        UnitId = unitId,
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken);
                return;
            }

            throw new InvalidOperationException(
                $"Cannot decrease stock: no balance row for warehouse {warehouseId}, item {stockItemId}.");
        }

        row.Quantity += delta;
        row.UnitId = unitId;
        row.LastModifiedAtUtc = DateTime.UtcNow;

        if (row.Quantity < 0)
            throw new InvalidOperationException("Stock quantity would become negative.");

        _warehouseStockRepository.Update(row);
    }
}
