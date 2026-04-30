using System.Text;
using Application.Common.Interfaces;
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
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IAuthenticatedUserAccessor _authenticatedUserAccessor;
    private readonly ILogger<SubmitStockRequestCommandHandler> _logger;

    public SubmitStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IWarehouseRepository warehouseRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        IEmailService emailService,
        INotificationService notificationService,
        IAuthenticatedUserAccessor authenticatedUserAccessor,
        ILogger<SubmitStockRequestCommandHandler> logger)
    {
        _stockRequestRepository = stockRequestRepository;
        _warehouseRepository = warehouseRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _emailService = emailService;
        _notificationService = notificationService;
        _authenticatedUserAccessor = authenticatedUserAccessor;
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

        var actingUserId = await _authenticatedUserAccessor.ResolveUserIdAsync(cancellationToken);

        var oldStatus = entity.Status;
        var lineCount = entity.Lines.Count;

        entity.Status = StockRequestStatus.Submitted;

        if ((entity.RequestedByUserId is null || entity.RequestedByUserId == 0) && actingUserId > 0)
            entity.RequestedByUserId = actingUserId;

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

        try
        {
            var docNo = $"SR-{entity.Id:D5}";
            var submitterId = actingUserId > 0 ? actingUserId : entity.RequestedByUserId ?? 0;

            if (submitterId > 0 && entity.CompanyId > 0)
            {
                try
                {
                    _logger.LogInformation(
                        "StockRequest submitted: Id={StockRequestId}, DocumentNo={DocumentNo}, CompanyId={CompanyId}. Creating submitter notification UserId={UserId}.",
                        entity.Id,
                        docNo,
                        entity.CompanyId,
                        submitterId);

                    await _notificationService.CreateAsync(
                        submitterId,
                        entity.CompanyId,
                        "Stok sorğusu göndərildi",
                        $"{docNo} nömrəli sorğu təsdiq üçün göndərildi.",
                        "StockRequest",
                        entity.Id,
                        "StockRequest",
                        actingUserId > 0 ? actingUserId : null,
                        cancellationToken);

                    _logger.LogInformation(
                        "StockRequest submit notification (submitter) committed. StockRequestId: {StockRequestId}, DocumentNo: {DocumentNo}, UserId: {UserId}, CompanyId: {CompanyId}",
                        entity.Id,
                        docNo,
                        submitterId,
                        entity.CompanyId);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogError(
                        notifyEx,
                        "StockRequest submit notification (submitter) failed. StockRequestId: {StockRequestId}",
                        entity.Id);
                }
            }
            else
            {
                _logger.LogWarning(
                    "StockRequest submit: no user id for in-app notification (submitter). StockRequestId: {StockRequestId}, DocumentNo: {DocumentNo}, ActingUserId: {ActingUserId}, RequestedByUserId: {RequestedByUserId}, CompanyId: {CompanyId}",
                    entity.Id,
                    docNo,
                    actingUserId,
                    entity.RequestedByUserId,
                    entity.CompanyId);
            }

            var supplyingWarehouse = await _warehouseRepository.GetByIdWithResponsibleAsync(
                entity.SupplyingWarehouseId,
                entity.CompanyId,
                cancellationToken);

            var requestingWarehouse = await _warehouseRepository.GetByIdAsync(
                entity.RequestingWarehouseId,
                cancellationToken);

            if (supplyingWarehouse is null)
            {
                _logger.LogWarning(
                    "Supplying warehouse tapılmadı. StockRequestId: {StockRequestId}, WarehouseId: {WarehouseId}",
                    entity.Id,
                    entity.SupplyingWarehouseId);
            }
            else
            {
                try
                {
                    var approverUserId = supplyingWarehouse.ResponsibleEmployee?.UserId;
                    if (approverUserId is > 0 && approverUserId != submitterId)
                    {
                        await _notificationService.CreateAsync(
                            approverUserId.Value,
                            entity.CompanyId,
                            "Təsdiq gözləyən stok sorğusu",
                            $"{docNo} nömrəli sorğu sizin təsdiqinizi gözləyir. İstəyən anbar: {requestingWarehouse?.Name ?? "—"}; Təchiz: {supplyingWarehouse.Name}.",
                            "StockRequest",
                            entity.Id,
                            "StockRequest",
                            actingUserId > 0 ? actingUserId : null,
                            cancellationToken);

                        _logger.LogInformation(
                            "StockRequest submit notification (approver). StockRequestId: {StockRequestId}, DocumentNo: {DocumentNo}, ApproverUserId: {ApproverUserId}, CompanyId: {CompanyId}",
                            entity.Id,
                            docNo,
                            approverUserId.Value,
                            entity.CompanyId);
                    }
                    else if (approverUserId is null or 0)
                    {
                        _logger.LogWarning(
                            "StockRequest submit: supplying warehouse responsible has no linked UserId (approver in-app skipped). StockRequestId: {StockRequestId}, WarehouseId: {WarehouseId}",
                            entity.Id,
                            supplyingWarehouse.Id);
                    }
                }
                catch (Exception notifyEx)
                {
                    _logger.LogError(
                        notifyEx,
                        "StockRequest submit notification (approver) failed. StockRequestId: {StockRequestId}",
                        entity.Id);
                }

                var approverEmail = supplyingWarehouse.ResponsibleEmployee?.Email;

                if (!string.IsNullOrWhiteSpace(approverEmail))
                {
                    var baseUrl = "https://localhost:7145";

                    var approveUrl = $"{baseUrl}/api/StockRequests/{entity.Id}/approve-from-mail";
                    var rejectUrl = $"{baseUrl}/api/StockRequests/{entity.Id}/reject-from-mail";

                    var linesHtml = new StringBuilder();

                    foreach (var line in entity.Lines)
                    {
                        linesHtml.Append($@"
<tr>
    <td style='border:1px solid #ddd;padding:8px;'>{line.StockItem?.Name ?? "-"}</td>
    <td style='border:1px solid #ddd;padding:8px;'>{line.Quantity:0.##}</td>
</tr>");
                    }

                    var body = $@"
<html>
<head>
    <meta charset='UTF-8' />
</head>
<body style='font-family:Arial,sans-serif;line-height:1.6;color:#212529;'>
    <h2>Yeni stok sorğusu təsdiq gözləyir</h2>

    <p><strong>Sorğu nömrəsi:</strong> #{entity.Id}</p>
    <p><strong>İstəyən anbar:</strong> {requestingWarehouse?.Name ?? "-"}</p>
    <p><strong>Təchiz edən anbar:</strong> {supplyingWarehouse.Name}</p>
    <p><strong>Qeyd:</strong> {entity.Note ?? "-"}</p>

    <h3>Stok siyahısı</h3>

    <table style='border-collapse:collapse;width:100%;max-width:700px;'>
        <thead>
            <tr style='background-color:#f8f9fa;'>
                <th style='border:1px solid #ddd;padding:8px;text-align:left;'>Stok adı</th>
                <th style='border:1px solid #ddd;padding:8px;text-align:left;'>Miqdarı</th>
            </tr>
        </thead>
        <tbody>
            {linesHtml}
        </tbody>
    </table>

    <br />

    <p>
        <a href='{approveUrl}' 
           style='display:inline-block;padding:10px 16px;background:#198754;color:white;text-decoration:none;border-radius:6px;'>
           Approve
        </a>

        &nbsp;

        <a href='{rejectUrl}' 
           style='display:inline-block;padding:10px 16px;background:#dc3545;color:white;text-decoration:none;border-radius:6px;'>
           Reject
        </a>
    </p>

    <p><strong>Approve link:</strong></p>
    <p>{approveUrl}</p>

    <p><strong>Reject link:</strong></p>
    <p>{rejectUrl}</p>
</body>
</html>";

                    await _emailService.SendAsync(
                        approverEmail,
                        $"Stock request approval #{entity.Id}",
                        body,
                        cancellationToken);

                    _logger.LogInformation(
                        "StockRequest submit email göndərildi. StockRequestId: {StockRequestId}, Email: {Email}",
                        entity.Id,
                        approverEmail);
                }
                else
                {
                    _logger.LogWarning(
                        "Approver email tapılmadı. StockRequestId: {StockRequestId}, WarehouseId: {WarehouseId}",
                        entity.Id,
                        supplyingWarehouse.Id);
                }
            }
        }
        catch (Exception emailEx)
        {
            _logger.LogError(
                emailEx,
                "StockRequest submit email göndərilərkən xəta baş verdi. StockRequestId: {StockRequestId}",
                entity.Id);
        }

        _logger.LogInformation(
            "Stock request uğurla submit olundu. StockRequestId: {StockRequestId}",
            entity.Id);

        return BaseResponse.Ok("Stock request submitted successfully.");
    }
}