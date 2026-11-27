using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Reports;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using MediatR;

namespace Application.StockAssets;

// ---- DTOs ----------------------------------------------------------------------------------

public class StockAssetWarehouseResponse
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public decimal Quantity { get; set; }
}

public class StockAssetResponse
{
    public int StockItemId { get; set; }
    public string Name { get; set; } = default!;
    public string? Barcode { get; set; }
    public int CategoryId { get; set; }
    public StockItemType Type { get; set; }
    public string CategoryName { get; set; } = default!;
    public UnitOfMeasure Unit { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal? AverageCostAzn { get; set; }

    /// <summary>Total quantity times the average cost (AZN); 0 when no cost is known.</summary>
    public decimal ValueAzn { get; set; }

    public List<StockAssetWarehouseResponse> Warehouses { get; set; } = new();
}

public class StockAssetListResponse
{
    public decimal TotalValueAzn { get; set; }
    public List<StockAssetResponse> Items { get; set; } = new();
}

public class StockAssetOperationResponse
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int StockItemId { get; set; }
    public string StockItemName { get; set; } = default!;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;

    /// <summary>Alış, Artım, Sınma, Xərcə silinmə, ...</summary>
    public string Operation { get; set; } = default!;

    /// <summary>True when the stock grew, false when it dropped.</summary>
    public bool IsIncrease { get; set; }

    public decimal Quantity { get; set; }
    public string? DocumentNo { get; set; }
    public string? UserName { get; set; }
    public string? Note { get; set; }
}

public class CreateStockAssetIncreaseRequest
{
    public int WarehouseId { get; set; }
    public int StockItemId { get; set; }
    public decimal Quantity { get; set; }
    public string? Note { get; set; }
}

// ---- Queries and commands ------------------------------------------------------------------

/// <summary>Non-food items with their stock; with <paramref name="AllTypes"/> every stock item (the stock page).</summary>
public record GetStockAssetsQuery(bool AllTypes = false) : IRequest<StockAssetListResponse>;

public record GetStockAssetHistoryQuery(DateTime? From, DateTime? To, int? StockItemId) : IRequest<List<StockAssetOperationResponse>>;

public record CreateStockAssetIncreaseCommand(CreateStockAssetIncreaseRequest Request) : IRequest<BaseResponse>;

public class StockAssetsHandlers :
    IRequestHandler<GetStockAssetsQuery, StockAssetListResponse>,
    IRequestHandler<GetStockAssetHistoryQuery, List<StockAssetOperationResponse>>,
    IRequestHandler<CreateStockAssetIncreaseCommand, BaseResponse>
{
    private readonly IStockAssetRepository _assets;
    private readonly IStockWriteOffRepository _writeOffs;
    private readonly IStockPurchaseRepository _purchases;
    private readonly IWarehouseStockRepository _warehouseStocks;
    private readonly IStockMovementRepository _movements;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public StockAssetsHandlers(
        IStockAssetRepository assets,
        IStockWriteOffRepository writeOffs,
        IStockPurchaseRepository purchases,
        IWarehouseStockRepository warehouseStocks,
        IStockMovementRepository movements,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _assets = assets;
        _writeOffs = writeOffs;
        _purchases = purchases;
        _warehouseStocks = warehouseStocks;
        _movements = movements;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<StockAssetListResponse> Handle(GetStockAssetsQuery query, CancellationToken ct)
    {
        var items = await _assets.GetAssetItemsAsync(_currentUser.CompanyId, query.AllTypes, ct);
        var rows = items.Select(i =>
        {
            var warehouses = i.WarehouseStocks
                .Where(w => w.Quantity != 0)
                .OrderBy(w => w.Warehouse.Name)
                .Select(w => new StockAssetWarehouseResponse { WarehouseId = w.WarehouseId, WarehouseName = w.Warehouse.Name, Quantity = w.Quantity })
                .ToList();
            var total = i.WarehouseStocks.Sum(w => w.Quantity);
            var cost = i.AverageCostAzn ?? i.LastPurchasePriceAzn;
            return new StockAssetResponse
            {
                StockItemId = i.Id,
                Name = i.Name,
                Barcode = i.Barcode,
                CategoryId = i.CategoryId,
                Type = i.Type,
                CategoryName = i.Category?.Name ?? string.Empty,
                Unit = i.Unit,
                TotalQuantity = total,
                AverageCostAzn = cost,
                ValueAzn = Math.Round(total * (cost ?? 0m), 2),
                Warehouses = warehouses,
            };
        }).ToList();

        return new StockAssetListResponse { TotalValueAzn = rows.Sum(r => r.ValueAzn), Items = rows };
    }

    public async Task<List<StockAssetOperationResponse>> Handle(GetStockAssetHistoryQuery query, CancellationToken ct)
    {
        DateTime? fromUtc = query.From is { } f ? ReportPeriodResolver.StoreFromUtc(f) : null;
        DateTime? toUtc = query.To is { } t ? ReportPeriodResolver.StoreToUtc(t) : null;

        var movements = await _assets.GetAssetMovementsAsync(_currentUser.CompanyId, fromUtc, toUtc, query.StockItemId, ct);
        var reasons = await _assets.GetWriteOffReasonsAsync(
            movements.Where(m => m.Type == StockMovementType.WriteOffOut && m.SourceId != null).Select(m => m.SourceId!.Value), ct);
        var names = await _writeOffs.GetUserNamesAsync(movements.Select(m => m.CreatedByUserId).OfType<int>(), ct);

        return movements.Select(m =>
        {
            StockWriteOffReason? reason = m.SourceId is { } id && reasons.TryGetValue(id, out var r) ? r : null;
            var (label, increase) = Describe(m.Type, reason);
            return new StockAssetOperationResponse
            {
                Id = m.Id,
                Date = m.MovementDate,
                StockItemId = m.StockItemId,
                StockItemName = m.StockItem?.Name ?? string.Empty,
                WarehouseId = m.WarehouseId,
                WarehouseName = m.Warehouse?.Name ?? string.Empty,
                Operation = label,
                IsIncrease = increase,
                Quantity = m.Quantity,
                DocumentNo = m.SourceDocumentNo,
                UserName = m.CreatedByUserId is { } uid && names.TryGetValue(uid, out var n) ? n : null,
                Note = m.Note,
            };
        }).ToList();
    }

    /// <summary>The operation's name and whether it grew the stock.</summary>
    internal static (string Label, bool IsIncrease) Describe(StockMovementType type, StockWriteOffReason? reason) => type switch
    {
        StockMovementType.PurchaseIn => ("Alış", true),
        StockMovementType.ManualIn => ("Artım", true),
        StockMovementType.StockEntryIn => ("Anbar mədaxili", true),
        StockMovementType.TransferIn => ("Transfer (daxil)", true),
        StockMovementType.SaleReturnIn => ("Satış qaytarması", true),
        StockMovementType.OrderConsumptionReversalIn => ("Sifariş ləğvi (geri)", true),
        StockMovementType.PurchaseReversalOut => ("Alışın ləğvi", false),
        StockMovementType.PurchaseReturnOut => ("Təchizatçıya qaytarma", false),
        StockMovementType.TransferOut => ("Transfer (çıxış)", false),
        StockMovementType.OrderConsumptionOut => ("Sifariş sərfi", false),
        StockMovementType.WriteOffOut => (reason switch
        {
            StockWriteOffReason.Broken => "Sınma",
            StockWriteOffReason.Expense => "Xərcə silinmə",
            StockWriteOffReason.Waste => "Tullantı",
            StockWriteOffReason.Spoiled => "Xarab olma",
            StockWriteOffReason.Expired => "Vaxtı keçib",
            StockWriteOffReason.Other => "Digər azalma",
            _ => "Azalma",
        }, false),
        _ => (type.ToString(), false),
    };

    public async Task<BaseResponse> Handle(CreateStockAssetIncreaseCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        if (request.Quantity <= 0)
            return Fail("Artım miqdarı 0-dan böyük olmalıdır.");
        if (!await _purchases.WarehouseExistsAsync(companyId, request.WarehouseId, ct))
            return Fail("Anbar tapılmadı.");

        var item = (await _purchases.GetStockItemsAsync(companyId, new[] { request.StockItemId }, ct)).FirstOrDefault();
        if (item is null)
            return Fail("Stok məhsulu tapılmadı.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var stock = await _warehouseStocks.GetOrCreateZeroBalanceAsync(
                companyId, request.WarehouseId, request.StockItemId, (int)item.Unit, userId, now, ct);
            stock.Quantity += request.Quantity;
            stock.LastModifiedAtUtc = now;
            stock.LastModifiedByUserId = userId;

            await _movements.AddAsync(new StockMovement
            {
                CompanyId = companyId,
                WarehouseId = request.WarehouseId,
                FromWarehouseId = null,
                ToWarehouseId = request.WarehouseId,
                StockItemId = request.StockItemId,
                Type = StockMovementType.ManualIn,
                SourceType = StockMovementSourceType.StockAdjustment,
                MovementDate = now,
                Quantity = request.Quantity,
                Note = string.IsNullOrWhiteSpace(request.Note) ? "Əl ilə artım" : request.Note.Trim(),
                CreatedByUserId = userId,
                CreatedAtUtc = now,
            }, ct);

            await _movements.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return new BaseResponse { Success = true, Message = "Stok artırıldı." };
    }

    private static BaseResponse Fail(string message) => new() { Success = false, Message = message };
}
