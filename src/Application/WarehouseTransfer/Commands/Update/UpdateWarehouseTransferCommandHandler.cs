using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseTransfer.Commands.Update;

public class UpdateWarehouseTransferCommandHandler
    : IRequestHandler<UpdateWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateWarehouseTransferCommandHandler> _logger;

    public UpdateWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IAuditLogService auditLogService,
        ILogger<UpdateWarehouseTransferCommandHandler> logger)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        UpdateWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpdateWarehouseTransferCommand başladı. TransferId: {TransferId}",
            request.Id);

        var transfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (transfer is null)
        {
            _logger.LogWarning(
                "Warehouse transfer update olunmadı. Transfer tapılmadı. TransferId: {TransferId}",
                request.Id);

            return BaseResponse.Fail("Warehouse transfer not found.");
        }

        if (transfer.Status != TransferStatus.Draft)
        {
            _logger.LogWarning(
                "Warehouse transfer update olunmadı. Status uyğun deyil. TransferId: {TransferId}, CurrentStatus: {CurrentStatus}",
                transfer.Id,
                transfer.Status);

            return BaseResponse.Fail("Only draft warehouse transfers can be updated.");
        }

        var fromWarehouseExists = await _warehouseTransferRepository
            .WarehouseExistsAsync(request.Request.FromWarehouseId, cancellationToken);

        if (!fromWarehouseExists)
        {
            _logger.LogWarning(
                "Warehouse transfer update olunmadı. From warehouse tapılmadı. TransferId: {TransferId}, FromWarehouseId: {FromWarehouseId}",
                transfer.Id,
                request.Request.FromWarehouseId);

            return BaseResponse.Fail("From warehouse not found.");
        }

        var toWarehouseExists = await _warehouseTransferRepository
            .WarehouseExistsAsync(request.Request.ToWarehouseId, cancellationToken);

        if (!toWarehouseExists)
        {
            _logger.LogWarning(
                "Warehouse transfer update olunmadı. To warehouse tapılmadı. TransferId: {TransferId}, ToWarehouseId: {ToWarehouseId}",
                transfer.Id,
                request.Request.ToWarehouseId);

            return BaseResponse.Fail("To warehouse not found.");
        }

        var stockItemIds = request.Request.Lines
            .Select(x => x.StockItemId)
            .Distinct()
            .ToList();

        var existingStockItemIds = await _warehouseTransferRepository
            .GetExistingStockItemIdsAsync(stockItemIds, cancellationToken);

        var missingStockItemIds = stockItemIds.Except(existingStockItemIds).ToList();

        if (missingStockItemIds.Any())
        {
            _logger.LogWarning(
                "Warehouse transfer update olunmadı. Stock item tapılmadı. TransferId: {TransferId}, MissingStockItemIds: {MissingStockItemIds}",
                transfer.Id,
                string.Join(", ", missingStockItemIds));

            return BaseResponse.Fail($"Stock item(s) not found: {string.Join(", ", missingStockItemIds)}");
        }

        var oldFromWarehouseId = transfer.FromWarehouseId;
        var oldToWarehouseId = transfer.ToWarehouseId;
        var oldNote = transfer.Note;
        var oldStatus = transfer.Status;
        var oldLineCount = transfer.Lines.Count;

        transfer.FromWarehouseId = request.Request.FromWarehouseId;
        transfer.ToWarehouseId = request.Request.ToWarehouseId;
        transfer.Note = request.Request.Note;

        if (transfer.Lines.Any())
            _warehouseTransferRepository.RemoveLines(transfer.Lines);

        transfer.Lines = request.Request.Lines
            .Select(x => new WarehouseTransferLine
            {
                CompanyId = transfer.CompanyId,
                StockItemId = x.StockItemId,
                Quantity = x.Quantity
            })
            .ToList();

        _warehouseTransferRepository.Update(transfer);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "WarehouseTransfer",
                    EntityId = transfer.Id.ToString(),
                    ActionType = "Update",
                    Message = $"WarehouseTransfer yeniləndi. Id: {transfer.Id}, OldFromWarehouseId: {oldFromWarehouseId}, NewFromWarehouseId: {transfer.FromWarehouseId}, OldToWarehouseId: {oldToWarehouseId}, NewToWarehouseId: {transfer.ToWarehouseId}, OldNote: {oldNote}, NewNote: {transfer.Note}, Status: {oldStatus}, OldLineCount: {oldLineCount}, NewLineCount: {transfer.Lines.Count}",
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
                "WarehouseTransfer update audit log yazılarkən xəta baş verdi. TransferId: {TransferId}",
                transfer.Id);
        }

        _logger.LogInformation(
            "Warehouse transfer uğurla yeniləndi. TransferId: {TransferId}",
            transfer.Id);

        return BaseResponse.Ok("Warehouse transfer updated successfully.");
    }
}