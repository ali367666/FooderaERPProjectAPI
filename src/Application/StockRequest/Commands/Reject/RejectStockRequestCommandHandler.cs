using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockRequests.Commands.Reject;

public class RejectStockRequestCommandHandler
    : IRequestHandler<RejectStockRequestCommand, BaseResponse>
{
    private readonly IStockRequestRepository _stockRequestRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;
    private readonly ILogger<RejectStockRequestCommandHandler> _logger;

    public RejectStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IWarehouseRepository warehouseRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        IEmailService emailService,
        ILogger<RejectStockRequestCommandHandler> logger)
    {
        _stockRequestRepository = stockRequestRepository;
        _warehouseRepository = warehouseRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        RejectStockRequestCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "RejectStockRequestCommand başladı. StockRequestId: {StockRequestId}",
            request.Id);

        var entity = await _stockRequestRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning(
                "Stock request reject olunmadı. StockRequest tapılmadı. StockRequestId: {StockRequestId}",
                request.Id);

            return BaseResponse.Fail("Stock request not found.");
        }

        if (entity.Status != StockRequestStatus.Submitted)
        {
            _logger.LogWarning(
                "Stock request reject olunmadı. Status uyğun deyil. StockRequestId: {StockRequestId}, CurrentStatus: {CurrentStatus}",
                request.Id,
                entity.Status);

            return BaseResponse.Fail("Only submitted requests can be rejected.");
        }

        var oldStatus = entity.Status;
        entity.Status = StockRequestStatus.Rejected;

        _stockRequestRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "StockRequest",
                    EntityId = entity.Id.ToString(),
                    ActionType = "Reject",
                    Message = $"StockRequest reject olundu. Id: {entity.Id}, OldStatus: {oldStatus}, NewStatus: {entity.Status}",
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
                "StockRequest reject audit log yazılarkən xəta baş verdi. StockRequestId: {StockRequestId}",
                entity.Id);
        }

        try
        {
            var requestingWarehouse = await _warehouseRepository.GetByIdWithResponsibleAsync(
                entity.RequestingWarehouseId,
                entity.CompanyId,
                cancellationToken);

            var requesterEmail = requestingWarehouse?.ResponsibleEmployee?.Email;

            if (!string.IsNullOrWhiteSpace(requesterEmail))
            {
                var subject = $"Stock request rejected #{entity.Id}";

                var body = $@"
<html>
<body style='font-family:Arial,sans-serif;line-height:1.6;'>
    <h2>Stock request rədd edildi</h2>
    <p><strong>Sorğu nömrəsi:</strong> #{entity.Id}</p>
    <p><strong>Status:</strong> {entity.Status}</p>
    <p>Sizin anbar tərəfindən göndərilən stok sorğusu rədd edildi.</p>
</body>
</html>";

                await _emailService.SendAsync(
                    requesterEmail,
                    subject,
                    body,
                    cancellationToken);

                _logger.LogInformation(
                    "Requester warehouse reject mail göndərildi. StockRequestId: {StockRequestId}, Email: {Email}",
                    entity.Id,
                    requesterEmail);
            }
            else
            {
                _logger.LogWarning(
                    "Requester warehouse email tapılmadı. StockRequestId: {StockRequestId}",
                    entity.Id);
            }
        }
        catch (Exception emailEx)
        {
            _logger.LogError(
                emailEx,
                "Reject sonrası requester mail göndərilərkən xəta baş verdi. StockRequestId: {StockRequestId}",
                entity.Id);
        }

        _logger.LogInformation(
            "Stock request uğurla reject olundu. StockRequestId: {StockRequestId}",
            entity.Id);

        return BaseResponse.Ok("Stock request rejected successfully.");
    }
}