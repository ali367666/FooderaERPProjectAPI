using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseTransfer.Commands.Delete;

public class DeleteWarehouseTransferCommandHandler
    : IRequestHandler<DeleteWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteWarehouseTransferCommandHandler> _logger;

    public DeleteWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IAuditLogService auditLogService,
        ILogger<DeleteWarehouseTransferCommandHandler> logger)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DeleteWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteWarehouseTransferCommand başladı. TransferId: {TransferId}",
            request.Id);

        var warehouseTransfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (warehouseTransfer is null)
        {
            _logger.LogWarning(
                "Warehouse transfer silinmədi. Transfer tapılmadı. TransferId: {TransferId}",
                request.Id);

            return new BaseResponse
            {
                Success = false,
                Message = "Warehouse transfer tapılmadı."
            };
        }

        if (warehouseTransfer.Status != TransferStatus.Draft)
        {
            _logger.LogWarning(
                "Warehouse transfer silinmədi. Status uyğun deyil. TransferId: {TransferId}, CurrentStatus: {CurrentStatus}",
                warehouseTransfer.Id,
                warehouseTransfer.Status);

            return new BaseResponse
            {
                Success = false,
                Message = "Yalnız Draft statusunda olan transfer silinə bilər."
            };
        }

        var oldCompanyId = warehouseTransfer.CompanyId;
        var oldStockRequestId = warehouseTransfer.StockRequestId;
        var oldFromWarehouseId = warehouseTransfer.FromWarehouseId;
        var oldToWarehouseId = warehouseTransfer.ToWarehouseId;
        var oldVehicleWarehouseId = warehouseTransfer.VehicleWarehouseId;
        var oldStatus = warehouseTransfer.Status;
        var oldNote = warehouseTransfer.Note;
        var oldLineCount = warehouseTransfer.Lines?.Count ?? 0;

        await _warehouseTransferRepository.DeleteAsync(warehouseTransfer, cancellationToken);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "WarehouseTransfer",
                    EntityId = warehouseTransfer.Id.ToString(),
                    ActionType = "Delete",
                    Message = $"WarehouseTransfer silindi. Id: {warehouseTransfer.Id}, CompanyId: {oldCompanyId}, StockRequestId: {oldStockRequestId}, FromWarehouseId: {oldFromWarehouseId}, ToWarehouseId: {oldToWarehouseId}, VehicleWarehouseId: {oldVehicleWarehouseId}, Status: {oldStatus}, Note: {oldNote}, LineCount: {oldLineCount}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "WarehouseTransfer üçün audit log yazıldı. TransferId: {TransferId}",
                warehouseTransfer.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "WarehouseTransfer delete audit log yazılarkən xəta baş verdi. TransferId: {TransferId}",
                warehouseTransfer.Id);
        }

        _logger.LogInformation(
            "Warehouse transfer uğurla silindi. TransferId: {TransferId}",
            warehouseTransfer.Id);

        return new BaseResponse
        {
            Success = true,
            Message = "Warehouse transfer uğurla silindi."
        };
    }
}