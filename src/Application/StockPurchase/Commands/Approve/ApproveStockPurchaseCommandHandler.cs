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

namespace Application.StockPurchase.Commands.Approve;

public class ApproveStockPurchaseCommandHandler
    : IRequestHandler<ApproveStockPurchaseCommand, BaseResponse>
{
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ApproveStockPurchaseCommandHandler> _logger;

    public ApproveStockPurchaseCommandHandler(
        IStockPurchaseRepository purchaseRepository,
        IWarehouseStockRepository warehouseStockRepository,
        IStockMovementRepository stockMovementRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<ApproveStockPurchaseCommandHandler> logger)
    {
        _purchaseRepository = purchaseRepository;
        _warehouseStockRepository = warehouseStockRepository;
        _stockMovementRepository = stockMovementRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(ApproveStockPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (purchase is null || !_currentUserService.CanAccessCompany(purchase.CompanyId))
            return new BaseResponse { Success = false, Message = "Stok alışı tapılmadı." };

        if (purchase.Status != StockPurchaseStatus.Pending)
            return new BaseResponse { Success = false, Message = "Yalnız gözləmədəki alışlar təsdiq edilə bilər." };

        if (purchase.Lines.Count == 0)
            return new BaseResponse { Success = false, Message = "Alışın sətri yoxdur." };

        var companyId = purchase.CompanyId;
        var userId = _currentUserService.UserId;
        var now = DateTime.UtcNow;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Cost first: the weighted average needs the quantity on hand BEFORE this purchase lands.
            var itemIds = purchase.Lines.Select(l => l.StockItemId).Distinct().ToList();
            var items = (await _purchaseRepository.GetStockItemsAsync(companyId, itemIds, cancellationToken))
                .ToDictionary(i => i.Id);

            foreach (var group in purchase.Lines.GroupBy(l => l.StockItemId))
            {
                if (!items.TryGetValue(group.Key, out var item))
                    continue;

                var quantity = group.Sum(l => l.Quantity);
                var cost = group.Sum(l => l.Quantity * l.UnitPriceAzn);
                var onHand = Math.Max(0, await _purchaseRepository.GetTotalStockQuantityAsync(companyId, group.Key, cancellationToken));

                // With no known earlier cost the average is simply this purchase's price.
                item.AverageCostAzn = item.AverageCostAzn is { } known && onHand > 0
                    ? Math.Round((onHand * known + cost) / (onHand + quantity), 4, MidpointRounding.AwayFromZero)
                    : Math.Round(cost / quantity, 4, MidpointRounding.AwayFromZero);
                item.LastPurchasePriceAzn = group.Last().UnitPriceAzn;
                item.LastModifiedAtUtc = now;
                item.LastModifiedByUserId = userId;
            }

            foreach (var line in purchase.Lines)
            {
                var stock = await _warehouseStockRepository.GetOrCreateZeroBalanceAsync(
                    companyId,
                    purchase.WarehouseId,
                    line.StockItemId,
                    unitId: (int)line.StockItem.Unit,
                    createdByUserId: userId,
                    utcNow: now,
                    cancellationToken);

                stock.Quantity += line.Quantity;

                await _stockMovementRepository.AddAsync(
                    new StockMovement
                    {
                        CompanyId = companyId,
                        WarehouseId = purchase.WarehouseId,
                        FromWarehouseId = null,
                        ToWarehouseId = purchase.WarehouseId,
                        StockItemId = line.StockItemId,
                        Type = StockMovementType.PurchaseIn,
                        SourceType = StockMovementSourceType.StockPurchase,
                        SourceId = purchase.Id,
                        SourceDocumentNo = purchase.DocumentNo,
                        MovementDate = now,
                        Quantity = line.Quantity,
                        Note = "Stock purchase approved",
                        CreatedByUserId = userId,
                        CreatedAtUtc = now,
                    },
                    cancellationToken);
            }

            purchase.Status = StockPurchaseStatus.Completed;
            purchase.ApprovedByUserId = userId;
            purchase.ApprovedAtUtc = now;
            purchase.LastModifiedByUserId = userId;
            purchase.LastModifiedAtUtc = now;
            _purchaseRepository.Update(purchase);

            // The status is a concurrency token: if another approval got there first, nothing is saved.
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
            _logger.LogError(ex, "ApproveStockPurchase failed. Id: {Id}", request.Id);
            throw;
        }

        await LogAsync(purchase, userId, cancellationToken);

        return new BaseResponse { Success = true, Message = "Stok alışı təsdiqləndi və anbar yeniləndi." };
    }

    private async Task LogAsync(Domain.Entities.WarehouseAndStock.StockPurchase purchase, int userId, CancellationToken cancellationToken)
    {
        try
        {
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                EntityName = "StockPurchase",
                EntityId = purchase.Id.ToString(),
                ActionType = "Approve",
                Message = $"StockPurchase təsdiqləndi. Id: {purchase.Id}, No: {purchase.DocumentNo}, WarehouseId: {purchase.WarehouseId}, UserId: {userId}",
                IsSuccess = true
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StockPurchase approve audit log xətası. Id: {Id}", purchase.Id);
        }
    }
}
