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
            "ReceiveWarehouseTransferCommand başladı. TransferId: {TransferId}",
            request.Id);

        var transfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (transfer is null)
        {
            _logger.LogWarning(
                "Warehouse transfer receive olunmadı. Transfer tapılmadı. TransferId: {TransferId}",
                request.Id);

            return BaseResponse.Fail("Warehouse transfer not found.");
        }

        if (transfer.Status != TransferStatus.InTransit)
        {
            _logger.LogWarning(
                "Warehouse transfer receive olunmadı. Status uyğun deyil. TransferId: {TransferId}, CurrentStatus: {CurrentStatus}",
                transfer.Id,
                transfer.Status);

            return BaseResponse.Fail("Only in-transit warehouse transfers can be received.");
        }

        if (transfer.Lines is null || !transfer.Lines.Any())
        {
            _logger.LogWarning(
                "Warehouse transfer receive olunmadı. Line yoxdur. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("Warehouse transfer has no lines.");
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
                        transfer.ToWarehouseId,
                        line.StockItemId,
                        cancellationToken);

                if (warehouseStock is null)
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
                    warehouseStock.QuantityOnHand += line.Quantity;
                    _warehouseStockRepository.Update(warehouseStock);
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

            _logger.LogError(
                ex,
                "Warehouse transfer receive zamanı xəta baş verdi. TransferId: {TransferId}",
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
                    ActionType = "Receive",
                    Message = $"WarehouseTransfer receive olundu. Id: {transfer.Id}, FromWarehouseId: {transfer.FromWarehouseId}, ToWarehouseId: {transfer.ToWarehouseId}, VehicleWarehouseId: {transfer.VehicleWarehouseId}, OldStatus: {oldStatus}, NewStatus: {transfer.Status}, LineCount: {lineCount}",
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
                "WarehouseTransfer receive audit log yazılarkən xəta baş verdi. TransferId: {TransferId}",
                transfer.Id);
        }

        _logger.LogInformation(
            "Warehouse transfer uğurla receive olundu. TransferId: {TransferId}",
            transfer.Id);

        return BaseResponse.Ok("Warehouse transfer received successfully.");
    }
}