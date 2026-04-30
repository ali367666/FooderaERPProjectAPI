using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockRequests.Commands.Create;

public class CreateStockRequestCommandHandler
    : IRequestHandler<CreateStockRequestCommand, BaseResponse<int>>
{
    private readonly IStockRequestRepository _stockRequestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly IAuthenticatedUserAccessor _authenticatedUserAccessor;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CreateStockRequestCommandHandler> _logger;

    public CreateStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        IAuthenticatedUserAccessor authenticatedUserAccessor,
        INotificationService notificationService,
        ILogger<CreateStockRequestCommandHandler> logger)
    {
        _stockRequestRepository = stockRequestRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _authenticatedUserAccessor = authenticatedUserAccessor;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<BaseResponse<int>> Handle(
        CreateStockRequestCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "CreateStockRequestCommand başladı. CompanyId: {CompanyId}, RequestingWarehouseId: {RequestingWarehouseId}, SupplyingWarehouseId: {SupplyingWarehouseId}",
            request.Request.CompanyId,
            request.Request.RequestingWarehouseId,
            request.Request.SupplyingWarehouseId);

        if (request.Request.RequestingWarehouseId == request.Request.SupplyingWarehouseId)
        {
            _logger.LogWarning(
                "StockRequest yaradılmadı. Eyni warehouse seçilib. CompanyId: {CompanyId}, RequestingWarehouseId: {RequestingWarehouseId}, SupplyingWarehouseId: {SupplyingWarehouseId}",
                request.Request.CompanyId,
                request.Request.RequestingWarehouseId,
                request.Request.SupplyingWarehouseId);

            return BaseResponse<int>.Fail("Requesting warehouse and supplying warehouse cannot be the same.");
        }

        if (request.Request.Lines is null || !request.Request.Lines.Any())
        {
            _logger.LogWarning(
                "StockRequest yaradılmadı. Line yoxdur. CompanyId: {CompanyId}",
                request.Request.CompanyId);

            return BaseResponse<int>.Fail("Stock request must contain at least one line.");
        }

        var actingUserId = await _authenticatedUserAccessor.ResolveUserIdAsync(cancellationToken);
        var entity = new Domain.Entities.WarehouseAndStock.StockRequest
        {
            CompanyId = request.Request.CompanyId,
            RequestingWarehouseId = request.Request.RequestingWarehouseId,
            SupplyingWarehouseId = request.Request.SupplyingWarehouseId,
            Status = StockRequestStatus.Draft,
            Note = request.Request.Note,
            RequestedByUserId = actingUserId > 0 ? actingUserId : null,
            Lines = request.Request.Lines.Select(x => new StockRequestLine
            {
                CompanyId = request.Request.CompanyId,
                StockItemId = x.StockItemId,
                Quantity = x.Quantity
            }).ToList()
        };

        await _stockRequestRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "StockRequest",
                    EntityId = entity.Id.ToString(),
                    ActionType = "Create",
                    Message = $"StockRequest yaradıldı. Id: {entity.Id}, CompanyId: {entity.CompanyId}, RequestingWarehouseId: {entity.RequestingWarehouseId}, SupplyingWarehouseId: {entity.SupplyingWarehouseId}, Status: {entity.Status}, Note: {entity.Note}, LineCount: {entity.Lines.Count}",
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
                "StockRequest create audit log yazılarkən xəta baş verdi. StockRequestId: {StockRequestId}",
                entity.Id);
        }

        try
        {
            var recipientId = actingUserId > 0 ? actingUserId : entity.RequestedByUserId ?? 0;
            var docNo = $"SR-{entity.Id:D5}";
            if (recipientId > 0 && entity.CompanyId > 0)
            {
                _logger.LogInformation(
                    "StockRequest created: Id={StockRequestId}, DocumentNo={DocumentNo}, CompanyId={CompanyId}. Creating notification for recipient UserId={RecipientId}.",
                    entity.Id,
                    docNo,
                    entity.CompanyId,
                    recipientId);

                await _notificationService.CreateAsync(
                    recipientId,
                    entity.CompanyId,
                    "Yeni stok sorğusu",
                    $"{docNo} nömrəli yeni stok sorğusu yaradıldı (draft, {entity.Lines.Count} sətir). Təsdiq üçün göndərin.",
                    "StockRequest",
                    entity.Id,
                    "StockRequest",
                    actingUserId > 0 ? actingUserId : null,
                    cancellationToken);

                _logger.LogInformation(
                    "StockRequest create notification committed. StockRequestId: {StockRequestId}, DocumentNo: {DocumentNo}, UserId: {UserId}, CompanyId: {CompanyId}",
                    entity.Id,
                    docNo,
                    recipientId,
                    entity.CompanyId);
            }
            else
            {
                _logger.LogWarning(
                    "StockRequest create notification atlandı (user/company context). StockRequestId: {StockRequestId}, DocumentNo: {DocumentNo}, ActingUserId: {ActingUserId}, RecipientId: {RecipientId}, CompanyId: {CompanyId}",
                    entity.Id,
                    docNo,
                    actingUserId,
                    recipientId,
                    entity.CompanyId);
            }
        }
        catch (Exception notifyEx)
        {
            _logger.LogError(
                notifyEx,
                "StockRequest create notification uğursuz. StockRequestId: {StockRequestId}",
                entity.Id);
        }

        _logger.LogInformation(
            "Stock request uğurla yaradıldı. StockRequestId: {StockRequestId}",
            entity.Id);

        return BaseResponse<int>.Ok(entity.Id, "Stock request created successfully.");
    }
}