using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseTransfer.Commands.Reject;

public class RejectWarehouseTransferCommandHandler
    : IRequestHandler<RejectWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<RejectWarehouseTransferCommandHandler> _logger;

    public RejectWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IAuditLogService auditLogService,
        ILogger<RejectWarehouseTransferCommandHandler> logger)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        RejectWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "RejectWarehouseTransferCommand başladı. TransferId: {TransferId}",
            request.Id);

        var transfer = await _warehouseTransferRepository
            .GetByIdAsync(request.Id, cancellationToken);

        if (transfer is null)
        {
            _logger.LogWarning(
                "Warehouse transfer reject olunmadı. Transfer tapılmadı. TransferId: {TransferId}",
                request.Id);

            return BaseResponse.Fail("Warehouse transfer not found.");
        }

        if (transfer.Status != TransferStatus.Pending)
        {
            _logger.LogWarning(
                "Warehouse transfer reject olunmadı. Status uyğun deyil. TransferId: {TransferId}, CurrentStatus: {CurrentStatus}",
                transfer.Id,
                transfer.Status);

            return BaseResponse.Fail("Only pending warehouse transfers can be rejected.");
        }

        var oldStatus = transfer.Status;

        transfer.Status = TransferStatus.Rejected;

        _warehouseTransferRepository.Update(transfer);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "WarehouseTransfer",
                    EntityId = transfer.Id.ToString(),
                    ActionType = "Reject",
                    Message = $"WarehouseTransfer reject olundu. Id: {transfer.Id}, FromWarehouseId: {transfer.FromWarehouseId}, ToWarehouseId: {transfer.ToWarehouseId}, VehicleWarehouseId: {transfer.VehicleWarehouseId}, OldStatus: {oldStatus}, NewStatus: {transfer.Status}",
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
                "WarehouseTransfer reject audit log yazılarkən xəta baş verdi. TransferId: {TransferId}",
                transfer.Id);
        }

        _logger.LogInformation(
            "Warehouse transfer uğurla reject olundu. TransferId: {TransferId}",
            transfer.Id);

        return BaseResponse.Ok("Warehouse transfer rejected successfully.");
    }
}