using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockRequests.Commands.Approve;

public class ApproveStockRequestCommandHandler
    : IRequestHandler<ApproveStockRequestCommand, BaseResponse>
{
    private readonly IStockRequestRepository _stockRequestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ApproveStockRequestCommandHandler> _logger;

    public ApproveStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<ApproveStockRequestCommandHandler> logger)
    {
        _stockRequestRepository = stockRequestRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        ApproveStockRequestCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ApproveStockRequestCommand başladı. StockRequestId: {StockRequestId}",
            request.Id);

        var entity = await _stockRequestRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning(
                "Stock request approve olunmadı. StockRequest tapılmadı. StockRequestId: {StockRequestId}",
                request.Id);

            return BaseResponse.Fail("Stock request not found.");
        }

        if (entity.Status != StockRequestStatus.Submitted)
        {
            _logger.LogWarning(
                "Stock request approve olunmadı. Status uyğun deyil. StockRequestId: {StockRequestId}, CurrentStatus: {CurrentStatus}",
                request.Id,
                entity.Status);

            return BaseResponse.Fail("Only submitted requests can be approved.");
        }

        var oldStatus = entity.Status;

        entity.Status = StockRequestStatus.Approved;

        _stockRequestRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "StockRequest",
                    EntityId = entity.Id.ToString(),
                    ActionType = "Approve",
                    Message = $"StockRequest approve olundu. Id: {entity.Id}, OldStatus: {oldStatus}, NewStatus: {entity.Status}",
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
                "StockRequest approve audit log yazılarkən xəta baş verdi. StockRequestId: {StockRequestId}",
                entity.Id);
        }

        _logger.LogInformation(
            "Stock request uğurla approve olundu. StockRequestId: {StockRequestId}",
            entity.Id);

        return BaseResponse.Ok("Stock request approved successfully.");
    }
}