using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockRequests.Commands.Submit;

public class SubmitStockRequestCommandHandler
    : IRequestHandler<SubmitStockRequestCommand, BaseResponse>
{
    private readonly IStockRequestRepository _stockRequestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SubmitStockRequestCommandHandler> _logger;

    public SubmitStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<SubmitStockRequestCommandHandler> logger)
    {
        _stockRequestRepository = stockRequestRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        SubmitStockRequestCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "SubmitStockRequestCommand başladı. StockRequestId: {StockRequestId}",
            request.Id);

        var entity = await _stockRequestRepository.GetByIdWithLinesAsync(
            request.Id,
            cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning(
                "Stock request submit olunmadı. StockRequest tapılmadı. StockRequestId: {StockRequestId}",
                request.Id);

            return BaseResponse.Fail("Stock request not found.");
        }

        if (entity.Status != StockRequestStatus.Draft)
        {
            _logger.LogWarning(
                "Stock request submit olunmadı. Status uyğun deyil. StockRequestId: {StockRequestId}, CurrentStatus: {CurrentStatus}",
                entity.Id,
                entity.Status);

            return BaseResponse.Fail("Only draft requests can be submitted.");
        }

        if (entity.Lines is null || !entity.Lines.Any())
        {
            _logger.LogWarning(
                "Stock request submit olunmadı. Line yoxdur. StockRequestId: {StockRequestId}",
                entity.Id);

            return BaseResponse.Fail("Stock request must contain at least one line.");
        }

        if (entity.RequestingWarehouseId == entity.SupplyingWarehouseId)
        {
            _logger.LogWarning(
                "Stock request submit olunmadı. Eyni warehouse seçilib. StockRequestId: {StockRequestId}, RequestingWarehouseId: {RequestingWarehouseId}, SupplyingWarehouseId: {SupplyingWarehouseId}",
                entity.Id,
                entity.RequestingWarehouseId,
                entity.SupplyingWarehouseId);

            return BaseResponse.Fail("Requesting warehouse and supplying warehouse cannot be the same.");
        }

        var oldStatus = entity.Status;
        var lineCount = entity.Lines.Count;

        entity.Status = StockRequestStatus.Submitted;

        _stockRequestRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "StockRequest",
                    EntityId = entity.Id.ToString(),
                    ActionType = "Submit",
                    Message = $"StockRequest submit olundu. Id: {entity.Id}, RequestingWarehouseId: {entity.RequestingWarehouseId}, SupplyingWarehouseId: {entity.SupplyingWarehouseId}, OldStatus: {oldStatus}, NewStatus: {entity.Status}, LineCount: {lineCount}",
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
                "StockRequest submit audit log yazılarkən xəta baş verdi. StockRequestId: {StockRequestId}",
                entity.Id);
        }

        _logger.LogInformation(
            "Stock request uğurla submit olundu. StockRequestId: {StockRequestId}",
            entity.Id);

        return BaseResponse.Ok("Stock request submitted successfully.");
    }
}