using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockRequests.Commands.Delete;

public class DeleteStockRequestCommandHandler
    : IRequestHandler<DeleteStockRequestCommand, BaseResponse>
{
    private readonly IStockRequestRepository _stockRequestRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteStockRequestCommandHandler> _logger;

    public DeleteStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IAuditLogService auditLogService,
        ILogger<DeleteStockRequestCommandHandler> logger)
    {
        _stockRequestRepository = stockRequestRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DeleteStockRequestCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteStockRequestCommand başladı. StockRequestId: {StockRequestId}",
            request.Id);

        var stockRequest = await _stockRequestRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (stockRequest is null)
        {
            _logger.LogWarning(
                "Stock request silinmədi. StockRequest tapılmadı. StockRequestId: {StockRequestId}",
                request.Id);

            return new BaseResponse
            {
                Success = false,
                Message = "Stock request tapılmadı."
            };
        }

        if (stockRequest.Status != StockRequestStatus.Draft)
        {
            _logger.LogWarning(
                "Stock request silinmədi. Status uyğun deyil. StockRequestId: {StockRequestId}, CurrentStatus: {CurrentStatus}",
                stockRequest.Id,
                stockRequest.Status);

            return new BaseResponse
            {
                Success = false,
                Message = "Yalnız Draft statusunda olan request silinə bilər."
            };
        }

        var oldCompanyId = stockRequest.CompanyId;
        var oldRequestingWarehouseId = stockRequest.RequestingWarehouseId;
        var oldSupplyingWarehouseId = stockRequest.SupplyingWarehouseId;
        var oldStatus = stockRequest.Status;
        var oldNote = stockRequest.Note;
        var oldLineCount = stockRequest.Lines?.Count ?? 0;

        await _stockRequestRepository.DeleteAsync(stockRequest, cancellationToken);
        await _stockRequestRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "StockRequest",
                    EntityId = stockRequest.Id.ToString(),
                    ActionType = "Delete",
                    Message = $"StockRequest silindi. Id: {stockRequest.Id}, CompanyId: {oldCompanyId}, RequestingWarehouseId: {oldRequestingWarehouseId}, SupplyingWarehouseId: {oldSupplyingWarehouseId}, Status: {oldStatus}, Note: {oldNote}, LineCount: {oldLineCount}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "StockRequest üçün audit log yazıldı. StockRequestId: {StockRequestId}",
                stockRequest.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "StockRequest delete audit log yazılarkən xəta baş verdi. StockRequestId: {StockRequestId}",
                stockRequest.Id);
        }

        _logger.LogInformation(
            "Stock request uğurla silindi. StockRequestId: {StockRequestId}",
            stockRequest.Id);

        return new BaseResponse
        {
            Success = true,
            Message = "Stock request uğurla silindi."
        };
    }
}