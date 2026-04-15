using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
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
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CreateStockRequestCommandHandler> _logger;

    public CreateStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        IWarehouseRepository warehouseRepository,
        INotificationService notificationService,
        ILogger<CreateStockRequestCommandHandler> logger)
    {
        _stockRequestRepository = stockRequestRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _warehouseRepository = warehouseRepository;
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

        var entity = new Domain.Entities.WarehouseAndStock.StockRequest
        {
            CompanyId = request.Request.CompanyId,
            RequestingWarehouseId = request.Request.RequestingWarehouseId,
            SupplyingWarehouseId = request.Request.SupplyingWarehouseId,
            Status = StockRequestStatus.Draft,
            Note = request.Request.Note,
            Lines = request.Request.Lines.Select(x => new Domain.Entities.WarehouseAndStock.StockRequestLine
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
            var supplyingWarehouse = await _warehouseRepository.GetByIdWithResponsibleAsync(
                request.Request.SupplyingWarehouseId,
                request.Request.CompanyId,
                cancellationToken);

            if (supplyingWarehouse is not null)
            {
                var responsibleEmployee = supplyingWarehouse.ResponsibleEmployee;

                if (responsibleEmployee?.UserId is not null)
                {
                    await _notificationService.CreateAsync(
                        userId: responsibleEmployee.UserId.Value,
                        companyId: request.Request.CompanyId,
                        title: "Yeni stok sorğusu",
                        message: $"{supplyingWarehouse.Name} anbarı üçün #{entity.Id} nömrəli yeni stok sorğusu yaradıldı.",
                        type: "StockRequest",
                        referenceId: entity.Id,
                        referenceType: "StockRequest",
                        cancellationToken: cancellationToken);

                    _logger.LogInformation(
                        "StockRequest notification yaradıldı. StockRequestId: {StockRequestId}, TargetUserId: {TargetUserId}",
                        entity.Id,
                        responsibleEmployee.UserId.Value);
                }
                else
                {
                    _logger.LogWarning(
                        "Supplying warehouse məsul işçisinin UserId-si tapılmadı. WarehouseId: {WarehouseId}, StockRequestId: {StockRequestId}",
                        supplyingWarehouse.Id,
                        entity.Id);
                }
            }
            else
            {
                _logger.LogWarning(
                    "Supplying warehouse tapılmadı. WarehouseId: {WarehouseId}, CompanyId: {CompanyId}, StockRequestId: {StockRequestId}",
                    request.Request.SupplyingWarehouseId,
                    request.Request.CompanyId,
                    entity.Id);
            }
        }
        catch (Exception notificationEx)
        {
            _logger.LogError(
                notificationEx,
                "StockRequest notification yaradılarkən xəta baş verdi. StockRequestId: {StockRequestId}",
                entity.Id);
        }

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

        _logger.LogInformation(
            "Stock request uğurla yaradıldı. StockRequestId: {StockRequestId}",
            entity.Id);

        return BaseResponse<int>.Ok(entity.Id, "Stock request created successfully.");
    }
}