using Application.Common.Interfaces;
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
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ApproveStockRequestCommandHandler> _logger;

    public ApproveStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IWarehouseRepository warehouseRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        IEmailService emailService,
        ILogger<ApproveStockRequestCommandHandler> logger)
    {
        _stockRequestRepository = stockRequestRepository;
        _warehouseRepository = warehouseRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _emailService = emailService;
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
                "Stock request approve olunmadı. Tapılmadı. StockRequestId: {StockRequestId}",
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

        // 🔵 Audit Log
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
                "Audit log yazıldı. StockRequestId: {StockRequestId}",
                entity.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "Audit log yazılarkən xəta. StockRequestId: {StockRequestId}",
                entity.Id);
        }

        // 🟢 MAIL → Requesting Warehouse
        try
        {
            var requestingWarehouse = await _warehouseRepository.GetByIdWithResponsibleAsync(
                entity.RequestingWarehouseId,
                entity.CompanyId,
                cancellationToken);

            var requesterEmail = requestingWarehouse?.ResponsibleEmployee?.Email;

            if (!string.IsNullOrWhiteSpace(requesterEmail))
            {
                var subject = $"Stock request approved #{entity.Id}";

                var body = $@"
<html>
<body style='font-family:Arial,sans-serif;line-height:1.6;'>
    <h2>Stock request təsdiq edildi</h2>
    <p><strong>Sorğu nömrəsi:</strong> #{entity.Id}</p>
    <p><strong>Status:</strong> {entity.Status}</p>
    <p>Sizin anbar tərəfindən göndərilən stok sorğusu təsdiq edildi.</p>
</body>
</html>";

                await _emailService.SendAsync(
                    requesterEmail,
                    subject,
                    body,
                    cancellationToken);

                _logger.LogInformation(
                    "Requester warehouse mail göndərildi. StockRequestId: {StockRequestId}, Email: {Email}",
                    entity.Id,
                    requesterEmail);
            }
            else
            {
                _logger.LogWarning(
                    "Requester email tapılmadı. StockRequestId: {StockRequestId}",
                    entity.Id);
            }
        }
        catch (Exception emailEx)
        {
            _logger.LogError(
                emailEx,
                "Approve sonrası mail göndərilərkən xəta. StockRequestId: {StockRequestId}",
                entity.Id);
        }

        _logger.LogInformation(
            "Stock request uğurla approve olundu. StockRequestId: {StockRequestId}",
            entity.Id);

        return BaseResponse.Ok("Stock request approved successfully.");
    }
}