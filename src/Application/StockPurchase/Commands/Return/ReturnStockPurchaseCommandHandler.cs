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

namespace Application.StockPurchase.Commands.Return;

public class ReturnStockPurchaseCommandHandler : IRequestHandler<ReturnStockPurchaseCommand, BaseResponse>
{
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ReturnStockPurchaseCommandHandler> _logger;

    public ReturnStockPurchaseCommandHandler(
        IStockPurchaseRepository purchaseRepository,
        IWarehouseStockRepository warehouseStockRepository,
        IStockMovementRepository stockMovementRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<ReturnStockPurchaseCommandHandler> logger)
    {
        _purchaseRepository = purchaseRepository;
        _warehouseStockRepository = warehouseStockRepository;
        _stockMovementRepository = stockMovementRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(ReturnStockPurchaseCommand command, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(command.Id, cancellationToken);

        if (purchase is null || !_currentUserService.CanAccessCompany(purchase.CompanyId))
            return Fail("Stok alışı tapılmadı.");

        if (purchase.Status != StockPurchaseStatus.Completed)
            return Fail("Qaytarma yalnız təsdiqlənmiş alışdan edilə bilər.");

        // One entry per purchase line, however many times it was typed.
        var requested = (command.Request.Lines ?? new())
            .GroupBy(l => l.StockPurchaseLineId)
            .Select(g => (LineId: g.Key, Quantity: g.Sum(x => x.Quantity)))
            .ToList();

        if (requested.Count == 0)
            return Fail("Qaytarılacaq ən azı bir sətir seçin.");
        if (requested.Any(r => r.Quantity <= 0))
            return Fail("Qaytarılan miqdar 0-dan böyük olmalıdır.");

        var returnedByLine = purchase.Returns.SelectMany(r => r.Lines)
            .GroupBy(l => l.StockPurchaseLineId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var returnLines = new List<StockPurchaseReturnLine>();
        foreach (var (lineId, quantity) in requested)
        {
            var line = purchase.Lines.FirstOrDefault(l => l.Id == lineId);
            if (line is null)
                return Fail("Seçilmiş sətir bu alışa aid deyil.");

            var returnable = line.Quantity - returnedByLine.GetValueOrDefault(lineId);
            if (quantity > returnable)
                return Fail($"\"{line.StockItem?.Name}\" üçün ən çox {returnable} qaytarmaq olar (alınıb: {line.Quantity}).");

            returnLines.Add(new StockPurchaseReturnLine
            {
                StockPurchaseLineId = line.Id,
                StockItemId = line.StockItemId,
                Quantity = quantity,
                UnitPriceAzn = line.UnitPriceAzn,
            });
        }

        var companyId = purchase.CompanyId;
        var userId = _currentUserService.UserId;
        var now = DateTime.UtcNow;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // The goods must still be in the warehouse: some may already have been sold, used or written off.
            foreach (var group in returnLines.GroupBy(l => l.StockItemId))
            {
                var needed = group.Sum(l => l.Quantity);
                var stock = await _warehouseStockRepository.GetByWarehouseAndStockItemAsync(
                    companyId, purchase.WarehouseId, group.Key, cancellationToken);

                if (stock is null || stock.Quantity < needed)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    var name = purchase.Lines.First(l => l.StockItemId == group.Key).StockItem?.Name ?? $"#{group.Key}";
                    return Fail($"\"{name}\" anbarda kifayət qədər yoxdur (lazımdır: {needed}, var: {stock?.Quantity ?? 0}).");
                }

                stock.Quantity -= needed;
                stock.LastModifiedAtUtc = now;
                stock.LastModifiedByUserId = userId;
            }

            foreach (var line in returnLines)
            {
                await _stockMovementRepository.AddAsync(
                    new StockMovement
                    {
                        CompanyId = companyId,
                        WarehouseId = purchase.WarehouseId,
                        FromWarehouseId = purchase.WarehouseId,
                        ToWarehouseId = null,
                        StockItemId = line.StockItemId,
                        Type = StockMovementType.PurchaseReturnOut,
                        SourceType = StockMovementSourceType.StockPurchaseReturn,
                        SourceId = purchase.Id,
                        SourceDocumentNo = purchase.DocumentNo,
                        MovementDate = now,
                        Quantity = line.Quantity,
                        Note = "Returned to supplier",
                        CreatedByUserId = userId,
                        CreatedAtUtc = now,
                    },
                    cancellationToken);
            }

            purchase.Returns.Add(new StockPurchaseReturn
            {
                CompanyId = companyId,
                Note = string.IsNullOrWhiteSpace(command.Request.Note) ? null : command.Request.Note.Trim(),
                Lines = returnLines,
                CreatedByUserId = userId,
                CreatedAtUtc = now,
            });
            purchase.LastModifiedByUserId = userId;
            purchase.LastModifiedAtUtc = now;
            _purchaseRepository.Update(purchase);

            if (!await _purchaseRepository.TrySaveChangesAsync(cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Fail("Bu alış artıq başqa istifadəçi tərəfindən dəyişdirilib. Səhifəni yeniləyin.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "ReturnStockPurchase failed. Id: {Id}", command.Id);
            throw;
        }

        var returnedValue = Math.Round(returnLines.Sum(l => l.Quantity * l.UnitPriceAzn), 2, MidpointRounding.AwayFromZero);
        var remaining = StockPurchaseMapper.Remaining(purchase);
        var overpaid = Math.Max(0, purchase.PaidAmountAzn - (StockPurchaseMapper.Total(purchase) - StockPurchaseMapper.ReturnedTotal(purchase)));

        try
        {
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                EntityName = "StockPurchase",
                EntityId = purchase.Id.ToString(),
                ActionType = "Return",
                Message = $"Təchizatçıya qaytarma: {returnedValue:0.00} AZN. Alış: {purchase.DocumentNo}, qalan borc: {remaining:0.00}, UserId: {userId}",
                IsSuccess = true
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StockPurchase return audit log xətası. Id: {Id}", purchase.Id);
        }

        var message = $"Mal təchizatçıya qaytarıldı ({returnedValue:0.00} AZN) və anbardan çıxarıldı. Qalan borc: {remaining:0.00} AZN.";
        if (overpaid > 0)
            message += $" Təchizatçı sizə {overpaid:0.00} AZN geri borcludur (artıq ödəniş).";
        return new BaseResponse { Success = true, Message = message };
    }

    private static BaseResponse Fail(string message) => new() { Success = false, Message = message };
}
