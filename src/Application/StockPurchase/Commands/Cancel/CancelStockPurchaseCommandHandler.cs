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

namespace Application.StockPurchase.Commands.Cancel;

public class CancelStockPurchaseCommandHandler : IRequestHandler<CancelStockPurchaseCommand, BaseResponse>
{
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CancelStockPurchaseCommandHandler> _logger;

    public CancelStockPurchaseCommandHandler(
        IStockPurchaseRepository purchaseRepository,
        IWarehouseStockRepository warehouseStockRepository,
        IStockMovementRepository stockMovementRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<CancelStockPurchaseCommandHandler> logger)
    {
        _purchaseRepository = purchaseRepository;
        _warehouseStockRepository = warehouseStockRepository;
        _stockMovementRepository = stockMovementRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(CancelStockPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (purchase is null || !_currentUserService.CanAccessCompany(purchase.CompanyId))
            return new BaseResponse { Success = false, Message = "Stok alışı tapılmadı." };

        if (purchase.Status != StockPurchaseStatus.Completed)
            return new BaseResponse { Success = false, Message = "Yalnız təsdiqlənmiş alışlar ləğv edilə bilər." };

        if (purchase.Returns.Count > 0)
            return new BaseResponse
            {
                Success = false,
                Message = "Bu alışdan təchizatçıya mal qaytarılıb, ona görə ləğv etmək olmur."
            };

        var companyId = purchase.CompanyId;
        var userId = _currentUserService.UserId;
        var now = DateTime.UtcNow;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // The goods must still be there: part of them may already have been sold, used or moved.
            foreach (var group in purchase.Lines.GroupBy(l => l.StockItemId))
            {
                var stock = await _warehouseStockRepository.GetByWarehouseAndStockItemAsync(
                    companyId, purchase.WarehouseId, group.Key, cancellationToken);
                var needed = group.Sum(l => l.Quantity);

                if (stock is null || stock.Quantity < needed)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    var name = group.First().StockItem?.Name ?? $"#{group.Key}";
                    return new BaseResponse
                    {
                        Success = false,
                        Message = $"\"{name}\" anbarda kifayət qədər yoxdur (lazımdır: {needed}, var: {stock?.Quantity ?? 0}). Alışı ləğv etmək olmur."
                    };
                }
            }

            foreach (var line in purchase.Lines)
            {
                var stock = (await _warehouseStockRepository.GetByWarehouseAndStockItemAsync(
                    companyId, purchase.WarehouseId, line.StockItemId, cancellationToken))!;

                stock.Quantity -= line.Quantity;
                stock.LastModifiedAtUtc = now;
                stock.LastModifiedByUserId = userId;

                await _stockMovementRepository.AddAsync(
                    new StockMovement
                    {
                        CompanyId = companyId,
                        WarehouseId = purchase.WarehouseId,
                        FromWarehouseId = purchase.WarehouseId,
                        ToWarehouseId = null,
                        StockItemId = line.StockItemId,
                        Type = StockMovementType.PurchaseReversalOut,
                        SourceType = StockMovementSourceType.StockPurchase,
                        SourceId = purchase.Id,
                        SourceDocumentNo = purchase.DocumentNo,
                        MovementDate = now,
                        Quantity = line.Quantity,
                        Note = "Stock purchase cancelled",
                        CreatedByUserId = userId,
                        CreatedAtUtc = now,
                    },
                    cancellationToken);
            }

            purchase.Status = StockPurchaseStatus.Cancelled;
            purchase.CancelledByUserId = userId;
            purchase.CancelledAtUtc = now;
            purchase.LastModifiedByUserId = userId;
            purchase.LastModifiedAtUtc = now;
            _purchaseRepository.Update(purchase);

            if (!await _purchaseRepository.TrySaveChangesAsync(cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new BaseResponse
                {
                    Success = false,
                    Message = "Bu alış artıq başqa istifadəçi tərəfindən dəyişdirilib. Səhifəni yeniləyin."
                };
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "CancelStockPurchase failed. Id: {Id}", request.Id);
            throw;
        }

        try
        {
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                EntityName = "StockPurchase",
                EntityId = purchase.Id.ToString(),
                ActionType = "Cancel",
                Message = $"StockPurchase ləğv edildi. Id: {purchase.Id}, No: {purchase.DocumentNo}, UserId: {userId}, Ödənilmiş: {purchase.PaidAmountAzn}",
                IsSuccess = true
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StockPurchase cancel audit log xətası. Id: {Id}", purchase.Id);
        }

        var message = "Stok alışı ləğv edildi və miqdarlar anbardan çıxarıldı.";
        if (purchase.PaidAmountAzn > 0)
            message += $" Təchizatçıya {purchase.PaidAmountAzn:0.00} AZN ödənilib, onu ayrıca həll edin.";
        return new BaseResponse { Success = true, Message = message };
    }
}
