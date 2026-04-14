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
            "DispatchWarehouseTransferCommand başladı. TransferId: {TransferId}",
            request.Id);

        var transfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (transfer is null)
        {
            _logger.LogWarning(
                "Warehouse transfer dispatch olunmadı. Transfer tapılmadı. TransferId: {TransferId}",
                request.Id);

            return BaseResponse.Fail("Warehouse transfer not found.");
        }

        if (transfer.Status != TransferStatus.Approved)
        {
            _logger.LogWarning(
                "Warehouse transfer dispatch olunmadı. Status uyğun deyil. TransferId: {TransferId}, CurrentStatus: {CurrentStatus}",
                transfer.Id,
                transfer.Status);

            return BaseResponse.Fail("Only approved warehouse transfers can be dispatched.");
        }

        if (transfer.Lines is null || !transfer.Lines.Any())
        {
            _logger.LogWarning(
                "Warehouse transfer dispatch olunmadı. Line yoxdur. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("Warehouse transfer has no lines.");
        }

        foreach (var line in transfer.Lines)
        {
            var warehouseStock = await _warehouseStockRepository
                .GetByWarehouseAndStockItemAsync(
                    transfer.FromWarehouseId,
                    line.StockItemId,
                    cancellationToken);

            if (warehouseStock is null)
            {
                _logger.LogWarning(
                    "Warehouse transfer dispatch olunmadı. Mənbə depoda stock tapılmadı. TransferId: {TransferId}, FromWarehouseId: {FromWarehouseId}, StockItemId: {StockItemId}",
                    transfer.Id,
                    transfer.FromWarehouseId,
                    line.StockItemId);

                return BaseResponse.Fail($"Stock item {line.StockItemId} was not found in source warehouse.");
            }

            if (warehouseStock.QuantityOnHand < line.Quantity)
            {
                _logger.LogWarning(
                    "Warehouse transfer dispatch olunmadı. Stock kifayət etmir. TransferId: {TransferId}, StockItemId: {StockItemId}, Available: {Available}, Requested: {Requested}",
                    transfer.Id,
                    line.StockItemId,
                    warehouseStock.QuantityOnHand,
                    line.Quantity);

                return BaseResponse.Fail(
                    $"Insufficient stock for stock item {line.StockItemId}. Available: {warehouseStock.QuantityOnHand}, Requested: {line.Quantity}");
            }
        }

        var oldStatus = transfer.Status;
        var lineCount = transfer.Lines.Count;

        await using var transaction = await _warehouseTransferRepository
            .BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var line in transfer.Lines)
            {
                var warehouseStock = await _warehouseStockRepository
                    .GetByWarehouseAndStockItemAsync(
                        transfer.FromWarehouseId,
                        line.StockItemId,
                        cancellationToken);

                warehouseStock!.QuantityOnHand -= line.Quantity;
                _warehouseStockRepository.Update(warehouseStock);
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

            _logger.LogError(
                ex,
                "Warehouse transfer dispatch zamanı xəta baş verdi. TransferId: {TransferId}",
                transfer.Id);

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
                    Message = $"WarehouseTransfer dispatch olundu. Id: {transfer.Id}, FromWarehouseId: {transfer.FromWarehouseId}, ToWarehouseId: {transfer.ToWarehouseId}, VehicleWarehouseId: {transfer.VehicleWarehouseId}, OldStatus: {oldStatus}, NewStatus: {transfer.Status}, LineCount: {lineCount}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "WarehouseTransfer üçün audit log yazıldı. TransferId: {TransferId}",
                transfer.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "WarehouseTransfer dispatch audit log yazılarkən xəta baş verdi. TransferId: {TransferId}",
                transfer.Id);
        }

        _logger.LogInformation(
            "Warehouse transfer uğurla dispatch olundu. TransferId: {TransferId}",
            transfer.Id);

        return BaseResponse.Ok("Warehouse transfer dispatched successfully.");
    }
}