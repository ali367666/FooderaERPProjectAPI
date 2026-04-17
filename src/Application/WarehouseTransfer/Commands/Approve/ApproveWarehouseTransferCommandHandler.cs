using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseTransfer.Commands.Approve;

public class ApproveWarehouseTransferCommandHandler
    : IRequestHandler<ApproveWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ApproveWarehouseTransferCommandHandler> _logger;

    public ApproveWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IAuditLogService auditLogService,
        ILogger<ApproveWarehouseTransferCommandHandler> logger)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        ApproveWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ApproveWarehouseTransferCommand başladı. TransferId: {TransferId}",
            request.Id);

        var transfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (transfer is null)
        {
            _logger.LogWarning(
                "Warehouse transfer approve olunmadı. Transfer tapılmadı. TransferId: {TransferId}",
                request.Id);

            return BaseResponse.Fail("Warehouse transfer not found.");
        }

        if (transfer.Status != TransferStatus.Pending)
        {
            _logger.LogWarning(
                "Warehouse transfer approve olunmadı. Status uyğun deyil. TransferId: {TransferId}, CurrentStatus: {CurrentStatus}",
                transfer.Id,
                transfer.Status);

            return BaseResponse.Fail("Only pending warehouse transfers can be approved.");
        }

        if (transfer.Lines is null || !transfer.Lines.Any())
        {
            _logger.LogWarning(
                "Warehouse transfer approve olunmadı. Line yoxdur. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("Warehouse transfer must contain at least one line.");
        }

        if (transfer.FromWarehouseId == transfer.ToWarehouseId)
        {
            _logger.LogWarning(
                "Warehouse transfer approve olunmadı. Eyni warehouse seçilib. TransferId: {TransferId}, FromWarehouseId: {FromWarehouseId}, ToWarehouseId: {ToWarehouseId}",
                transfer.Id,
                transfer.FromWarehouseId,
                transfer.ToWarehouseId);

            return BaseResponse.Fail("From warehouse and To warehouse cannot be the same.");
        }

        if (!transfer.VehicleWarehouseId.HasValue)
        {
            _logger.LogWarning(
                "Warehouse transfer approve olunmadı. VehicleWarehouse seçilməyib. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("Vehicle warehouse is required.");
        }

        if (transfer.VehicleWarehouseId.Value == transfer.FromWarehouseId ||
            transfer.VehicleWarehouseId.Value == transfer.ToWarehouseId)
        {
            _logger.LogWarning(
                "Warehouse transfer approve olunmadı. Vehicle warehouse uyğun deyil. TransferId: {TransferId}, VehicleWarehouseId: {VehicleWarehouseId}",
                transfer.Id,
                transfer.VehicleWarehouseId.Value);

            return BaseResponse.Fail("Vehicle warehouse cannot be the same as From or To warehouse.");
        }

        if (transfer.Lines.Any(x => x.Quantity <= 0))
        {
            _logger.LogWarning(
                "Warehouse transfer approve olunmadı. Quantity 0 və ya mənfidir. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("All line quantities must be greater than 0.");
        }

        var duplicateStockItemIds = transfer.Lines
            .GroupBy(x => x.StockItemId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateStockItemIds.Any())
        {
            _logger.LogWarning(
                "Warehouse transfer approve olunmadı. Duplicate StockItem var. TransferId: {TransferId}, DuplicateStockItemIds: {DuplicateStockItemIds}",
                transfer.Id,
                string.Join(", ", duplicateStockItemIds));

            return BaseResponse.Fail("The same StockItem cannot be added more than once in a transfer.");
        }

        var oldStatus = transfer.Status;
        var lineCount = transfer.Lines.Count;

        transfer.Status = TransferStatus.Approved;

        _warehouseTransferRepository.Update(transfer);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "WarehouseTransfer",
                    EntityId = transfer.Id.ToString(),
                    ActionType = "Approve",
                    Message = $"WarehouseTransfer approve olundu. Id: {transfer.Id}, FromWarehouseId: {transfer.FromWarehouseId}, ToWarehouseId: {transfer.ToWarehouseId}, VehicleWarehouseId: {transfer.VehicleWarehouseId}, OldStatus: {oldStatus}, NewStatus: {transfer.Status}, LineCount: {lineCount}",
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
                "WarehouseTransfer approve audit log yazılarkən xəta baş verdi. TransferId: {TransferId}",
                transfer.Id);
        }

        _logger.LogInformation(
            "Warehouse transfer uğurla approve olundu. TransferId: {TransferId}",
            transfer.Id);

        return BaseResponse.Ok("Warehouse transfer approved successfully.");
    }
}