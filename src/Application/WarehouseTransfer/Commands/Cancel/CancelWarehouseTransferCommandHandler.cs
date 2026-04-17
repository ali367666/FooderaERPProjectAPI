using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseTransfer.Commands.Cancel;

public class CancelWarehouseTransferCommandHandler
    : IRequestHandler<CancelWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CancelWarehouseTransferCommandHandler> _logger;

    public CancelWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IAuditLogService auditLogService,
        ILogger<CancelWarehouseTransferCommandHandler> logger)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        CancelWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "CancelWarehouseTransferCommand başladı. TransferId: {TransferId}",
            request.Id);

        var transfer = await _warehouseTransferRepository
            .GetByIdAsync(request.Id, cancellationToken);

        if (transfer is null)
        {
            _logger.LogWarning(
                "Warehouse transfer cancel olunmadı. Transfer tapılmadı. TransferId: {TransferId}",
                request.Id);

            return BaseResponse.Fail("Warehouse transfer not found.");
        }

        if (transfer.Status == TransferStatus.Cancelled)
        {
            _logger.LogWarning(
                "Warehouse transfer cancel olunmadı. Artıq Cancelled-dir. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("Warehouse transfer is already cancelled.");
        }

        if (transfer.Status == TransferStatus.Rejected)
        {
            _logger.LogWarning(
                "Warehouse transfer cancel olunmadı. Status Rejected-dir. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("Rejected warehouse transfers cannot be cancelled.");
        }

        if (transfer.Status == TransferStatus.InTransit)
        {
            _logger.LogWarning(
                "Warehouse transfer cancel olunmadı. Status InTransit-dir. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("In-transit warehouse transfers cannot be cancelled.");
        }

        if (transfer.Status == TransferStatus.Completed)
        {
            _logger.LogWarning(
                "Warehouse transfer cancel olunmadı. Status Completed-dir. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("Completed warehouse transfers cannot be cancelled.");
        }

        var oldStatus = transfer.Status;

        transfer.Status = TransferStatus.Cancelled;

        _warehouseTransferRepository.Update(transfer);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "WarehouseTransfer",
                    EntityId = transfer.Id.ToString(),
                    ActionType = "Cancel",
                    Message = $"WarehouseTransfer cancel olundu. Id: {transfer.Id}, FromWarehouseId: {transfer.FromWarehouseId}, ToWarehouseId: {transfer.ToWarehouseId}, VehicleWarehouseId: {transfer.VehicleWarehouseId}, OldStatus: {oldStatus}, NewStatus: {transfer.Status}",
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
                "WarehouseTransfer cancel audit log yazılarkən xəta baş verdi. TransferId: {TransferId}",
                transfer.Id);
        }

        _logger.LogInformation(
            "Warehouse transfer uğurla cancel olundu. TransferId: {TransferId}",
            transfer.Id);

        return BaseResponse.Ok("Warehouse transfer cancelled successfully.");
    }
}