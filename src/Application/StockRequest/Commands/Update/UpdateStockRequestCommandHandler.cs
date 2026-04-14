using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockRequests.Commands.Update;

public class UpdateStockRequestCommandHandler
    : IRequestHandler<UpdateStockRequestCommand, BaseResponse>
{
    private readonly IStockRequestRepository _stockRequestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateStockRequestCommandHandler> _logger;

    public UpdateStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<UpdateStockRequestCommandHandler> logger)
    {
        _stockRequestRepository = stockRequestRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        UpdateStockRequestCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpdateStockRequestCommand başladı. StockRequestId: {StockRequestId}",
            request.Id);

        var entity = await _stockRequestRepository.GetByIdWithLinesAsync(
            request.Id,
            cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning(
                "Stock request update olunmadı. StockRequest tapılmadı. StockRequestId: {StockRequestId}",
                request.Id);

            return BaseResponse.Fail("Stock request not found.");
        }

        if (entity.Status != StockRequestStatus.Draft)
        {
            _logger.LogWarning(
                "Stock request update olunmadı. Status uyğun deyil. StockRequestId: {StockRequestId}, CurrentStatus: {CurrentStatus}",
                entity.Id,
                entity.Status);

            return BaseResponse.Fail("Only draft requests can be updated.");
        }

        var oldRequestingWarehouseId = entity.RequestingWarehouseId;
        var oldSupplyingWarehouseId = entity.SupplyingWarehouseId;
        var oldNote = entity.Note;
        var oldStatus = entity.Status;
        var oldLineCount = entity.Lines.Count;

        entity.RequestingWarehouseId = request.Request.RequestingWarehouseId;
        entity.SupplyingWarehouseId = request.Request.SupplyingWarehouseId;
        entity.Note = request.Request.Note;

        var incomingLines = request.Request.Lines;

        var incomingLineIds = incomingLines
            .Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value)
            .ToHashSet();

        var existingLines = entity.Lines.ToList();

        var linesToRemove = existingLines
            .Where(x => !incomingLineIds.Contains(x.Id))
            .ToList();

        foreach (var line in linesToRemove)
        {
            entity.Lines.Remove(line);
        }

        foreach (var incomingLine in incomingLines)
        {
            if (incomingLine.Id.HasValue)
            {
                var existingLine = existingLines
                    .FirstOrDefault(x => x.Id == incomingLine.Id.Value);

                if (existingLine is null)
                {
                    _logger.LogWarning(
                        "Stock request update olunmadı. Line tapılmadı. StockRequestId: {StockRequestId}, LineId: {LineId}",
                        entity.Id,
                        incomingLine.Id.Value);

                    return BaseResponse.Fail($"Stock request line with id {incomingLine.Id.Value} not found.");
                }

                existingLine.StockItemId = incomingLine.StockItemId;
                existingLine.Quantity = incomingLine.Quantity;
            }
            else
            {
                entity.Lines.Add(new Domain.Entities.WarehouseAndStock.StockRequestLine
                {
                    CompanyId = entity.CompanyId,
                    StockItemId = incomingLine.StockItemId,
                    Quantity = incomingLine.Quantity
                });
            }
        }

        _stockRequestRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "StockRequest",
                    EntityId = entity.Id.ToString(),
                    ActionType = "Update",
                    Message = $"StockRequest yeniləndi. Id: {entity.Id}, OldRequestingWarehouseId: {oldRequestingWarehouseId}, NewRequestingWarehouseId: {entity.RequestingWarehouseId}, OldSupplyingWarehouseId: {oldSupplyingWarehouseId}, NewSupplyingWarehouseId: {entity.SupplyingWarehouseId}, OldNote: {oldNote}, NewNote: {entity.Note}, Status: {oldStatus}, OldLineCount: {oldLineCount}, NewLineCount: {entity.Lines.Count}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "StockRequest üçün audit log yazıldı. StockRequestId: {StockRequestId}",
                entity.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "StockRequest update audit log yazılarkən xəta baş verdi. StockRequestId: {StockRequestId}",
                entity.Id);
        }

        _logger.LogInformation(
            "Stock request uğurla yeniləndi. StockRequestId: {StockRequestId}",
            entity.Id);

        return BaseResponse.Ok("Stock request updated successfully.");
    }
}