using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseTransfer.Commands.Submit;

public class SubmitWarehouseTransferCommandHandler
    : IRequestHandler<SubmitWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SubmitWarehouseTransferCommandHandler> _logger;

    public SubmitWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IAuditLogService auditLogService,
        ILogger<SubmitWarehouseTransferCommandHandler> logger)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        SubmitWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "SubmitWarehouseTransferCommand başladı. TransferId: {TransferId}",
            request.Id);

        var transfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (transfer is null)
        {
            _logger.LogWarning(
                "Warehouse transfer submit olunmadı. Transfer tapılmadı. TransferId: {TransferId}",
                request.Id);

            return BaseResponse.Fail("Warehouse transfer not found.");
        }

        if (transfer.Status != TransferStatus.Draft)
        {
            _logger.LogWarning(
                "Warehouse transfer submit olunmadı. Status uyğun deyil. TransferId: {TransferId}, CurrentStatus: {CurrentStatus}",
                transfer.Id,
                transfer.Status);

            return BaseResponse.Fail("Only draft warehouse transfers can be submitted.");
        }

        if (transfer.FromWarehouseId == transfer.ToWarehouseId)
        {
            _logger.LogWarning(
                "Warehouse transfer submit olunmadı. Eyni warehouse seçilib. TransferId: {TransferId}, FromWarehouseId: {FromWarehouseId}, ToWarehouseId: {ToWarehouseId}",
                transfer.Id,
                transfer.FromWarehouseId,
                transfer.ToWarehouseId);

            return BaseResponse.Fail("From warehouse and To warehouse cannot be the same.");
        }

        if (transfer.Lines is null || !transfer.Lines.Any())
        {
            _logger.LogWarning(
                "Warehouse transfer submit olunmadı. Line yoxdur. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("Warehouse transfer must contain at least one line.");
        }

        if (transfer.Lines.Any(x => x.Quantity <= 0))
        {
            _logger.LogWarning(
                "Warehouse transfer submit olunmadı. Quantity 0 və ya mənfidir. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("All line quantities must be greater than 0.");
        }

        var oldStatus = transfer.Status;
        var lineCount = transfer.Lines.Count;

        transfer.Status = TransferStatus.Pending;

        _warehouseTransferRepository.Update(transfer);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "WarehouseTransfer",
                    EntityId = transfer.Id.ToString(),
                    ActionType = "Submit",
                    Message = $"WarehouseTransfer submit olundu. Id: {transfer.Id}, FromWarehouseId: {transfer.FromWarehouseId}, ToWarehouseId: {transfer.ToWarehouseId}, VehicleWarehouseId: {transfer.VehicleWarehouseId}, OldStatus: {oldStatus}, NewStatus: {transfer.Status}, LineCount: {lineCount}",
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
                "WarehouseTransfer submit audit log yazılarkən xəta baş verdi. TransferId: {TransferId}",
                transfer.Id);
        }

        _logger.LogInformation(
            "Warehouse transfer uğurla submit olundu. TransferId: {TransferId}",
            transfer.Id);

        return BaseResponse.Ok("Warehouse transfer submitted successfully.");
    }
}