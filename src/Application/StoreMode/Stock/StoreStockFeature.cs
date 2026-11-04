using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.StoreMode.Stock;

public record GetStoreStockQuery(int? WarehouseId, string? Term, bool LowOnly) : IRequest<List<StoreStockBalanceResponse>>;

public record GetStoreMovementsQuery(int? WarehouseId, int? ProductId, DateTime? From, DateTime? To, int Take)
    : IRequest<List<StoreStockMovementResponse>>;

public record AdjustStoreStockCommand(AdjustStoreStockRequest Request) : IRequest<StoreStockBalanceResponse>;

public record GetStoreSettingsQuery : IRequest<StoreSettingsResponse>;

public record UpdateStoreSettingsCommand(StoreSettingsResponse Request) : IRequest<StoreSettingsResponse>;

public class StoreStockHandlers :
    IRequestHandler<GetStoreStockQuery, List<StoreStockBalanceResponse>>,
    IRequestHandler<GetStoreMovementsQuery, List<StoreStockMovementResponse>>,
    IRequestHandler<AdjustStoreStockCommand, StoreStockBalanceResponse>,
    IRequestHandler<GetStoreSettingsQuery, StoreSettingsResponse>,
    IRequestHandler<UpdateStoreSettingsCommand, StoreSettingsResponse>
{
    private static readonly StoreStockMovementType[] AdjustTypes =
    {
        StoreStockMovementType.AdjustmentIncrease,
        StoreStockMovementType.AdjustmentDecrease,
        StoreStockMovementType.Waste,
        StoreStockMovementType.InternalUse,
    };

    private readonly IStoreRepository _repository;
    private readonly ICurrentUserService _currentUser;

    private readonly Application.StoreMode.Access.StoreAccessService _access;

    public StoreStockHandlers(IStoreRepository repository, ICurrentUserService currentUser, Application.StoreMode.Access.StoreAccessService access)
    {
        _access = access;
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<List<StoreStockBalanceResponse>> Handle(GetStoreStockQuery query, CancellationToken ct)
    {
        if (query.WarehouseId is { } wid) await _access.EnsureWarehouseAsync(wid, ct);
        var rows = await _repository.SearchBalancesAsync(
            _currentUser.CompanyId, query.WarehouseId, query.Term, query.LowOnly, ct);
        if (await _access.AllowedWarehousesAsync(ct) is { } allowedWarehouses)
            rows = rows.Where(r => allowedWarehouses.Contains(r.WarehouseId)).ToList();
        return rows.Select(Map).ToList();
    }

    public async Task<List<StoreStockMovementResponse>> Handle(GetStoreMovementsQuery query, CancellationToken ct)
    {
        // Days follow the same business-day cut-off as the other reports.
        DateTime? fromUtc = query.From is { } f ? BusinessTime.ToUtc(f.Date + ReportPeriodResolver.DayStart) : null;
        DateTime? toUtc = query.To is { } t ? BusinessTime.ToUtc(t.Date.AddDays(1) + ReportPeriodResolver.DayStart) : null;

        if (query.WarehouseId is { } wid) await _access.EnsureWarehouseAsync(wid, ct);
        var rows = await _repository.SearchMovementsAsync(
            _currentUser.CompanyId, query.WarehouseId, query.ProductId, fromUtc, toUtc, Math.Clamp(query.Take, 1, 500), ct);
        if (await _access.AllowedWarehousesAsync(ct) is { } allowedWarehouses)
            rows = rows.Where(r => allowedWarehouses.Contains(r.WarehouseId)).ToList();
        var names = await _repository.GetUserNamesAsync(rows.Select(r => r.CreatedByUserId).OfType<int>(), ct);

        return rows.Select(m => new StoreStockMovementResponse
        {
            Id = m.Id,
            CreatedAtUtc = m.CreatedAtUtc,
            CreatedByUserName = m.CreatedByUserId is { } id && names.TryGetValue(id, out var n) ? n : null,
            WarehouseId = m.WarehouseId,
            WarehouseName = m.Warehouse?.Name ?? string.Empty,
            ProductId = m.StoreProductId,
            ProductName = m.StoreProduct?.Name ?? string.Empty,
            Type = m.Type,
            TypeName = m.Type.ToString(),
            Quantity = m.Quantity,
            BalanceBefore = m.BalanceBefore,
            BalanceAfter = m.BalanceAfter,
            UnitCost = m.UnitCost,
            DocumentNo = m.DocumentNo,
            Reason = m.Reason,
        }).ToList();
    }

    public async Task<StoreStockBalanceResponse> Handle(AdjustStoreStockCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;

        if (!AdjustTypes.Contains(dto.Type))
            throw new BadRequestException("Düzəliş növü yanlışdır.");
        if (dto.Quantity <= 0)
            throw new BadRequestException("Miqdar 0-dan böyük olmalıdır.");
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new BadRequestException("Səbəb yazılmalıdır.");
        if (dto.UnitCost < 0)
            throw new BadRequestException("Maya dəyəri mənfi ola bilməz.");
        if (!await _repository.WarehouseExistsAsync(companyId, dto.WarehouseId, ct))
            throw new BadRequestException("Anbar tapılmadı.");

        var product = await _repository.GetProductAsync(dto.ProductId, companyId, ct)
            ?? throw new BadRequestException("Məhsul tapılmadı.");
        if (product.Unit == UnitOfMeasure.Piece && dto.Quantity != Math.Floor(dto.Quantity))
            throw new BadRequestException("Ədədlə satılan məhsulun miqdarı tam olmalıdır.");

        var increase = dto.Type == StoreStockMovementType.AdjustmentIncrease;
        var balance = await _repository.GetOrCreateBalanceAsync(companyId, dto.WarehouseId, dto.ProductId, ct);

        if (!increase)
        {
            var settings = await _repository.GetOrCreateSettingsAsync(companyId, ct);
            if (!settings.AllowNegativeStock && balance.Quantity < dto.Quantity)
                throw new BadRequestException(
                    $"\"{product.Name}\" anbarda kifayət qədər yoxdur (lazımdır: {dto.Quantity}, var: {balance.Quantity}).");
        }

        // Serial / IMEI tracked products move unit by unit.
        var serials = (dto.SerialNumbers ?? new()).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        if (product.TracksSerial)
        {
            if (dto.Quantity != Math.Floor(dto.Quantity) || serials.Count != (int)dto.Quantity)
                throw new BadRequestException($"\"{product.Name}\" üçün {dto.Quantity:0.##} ədəd IMEI/serial nömrəsi yazılmalıdır (yazılıb: {serials.Count}).");
            if (serials.Distinct().Count() != serials.Count)
                throw new BadRequestException("Eyni IMEI/serial nömrəsi iki dəfə yazılıb.");
        }
        else if (serials.Count > 0)
        {
            throw new BadRequestException($"\"{product.Name}\" IMEI/serial ilə izlənmir.");
        }

        // Added goods with a known cost move the weighted average cost.
        if (increase && dto.UnitCost is { } cost)
        {
            var onHand = Math.Max(0, await _repository.GetTotalQuantityAsync(companyId, product.Id, ct));
            product.AverageCost = Math.Round(
                (onHand * product.AverageCost + dto.Quantity * cost) / (onHand + dto.Quantity), 4, MidpointRounding.AwayFromZero);
        }

        var before = balance.Quantity;
        balance.Quantity += increase ? dto.Quantity : -dto.Quantity;

        if (product.TracksSerial)
        {
            var existing = await _repository.FindSerialUnitsAsync(companyId, serials, ct);
            if (increase)
            {
                if (existing.Count > 0)
                    throw new BadRequestException($"\"{existing[0].SerialNumber}\" nömrəsi artıq sistemdə var (IMEI/serial təkrar ola bilməz).");
                foreach (var serial in serials)
                {
                    var unit = new StoreSerialUnit
                    {
                        CompanyId = companyId,
                        StoreProductId = product.Id,
                        SerialNumber = serial,
                        WarehouseId = dto.WarehouseId,
                        Status = StoreSerialStatus.InStock,
                        CreatedByUserId = _currentUser.UserId,
                    };
                    unit.Events.Add(new StoreSerialEvent
                    {
                        Type = StoreSerialEventType.Received,
                        WarehouseId = dto.WarehouseId,
                        DocumentType = "Adjustment",
                        Note = dto.Reason.Trim(),
                        CreatedByUserId = _currentUser.UserId,
                    });
                    await _repository.AddSerialUnitAsync(unit, ct);
                }
            }
            else
            {
                foreach (var serial in serials)
                {
                    var unit = existing.FirstOrDefault(u => u.SerialNumber == serial);
                    if (unit is null || unit.StoreProductId != product.Id)
                        throw new BadRequestException($"\"{serial}\" nömrəsi bu məhsul üçün tapılmadı.");
                    if (unit.Status != StoreSerialStatus.InStock || unit.WarehouseId != dto.WarehouseId)
                        throw new BadRequestException($"\"{serial}\" bu anbarda stokda deyil.");

                    unit.Status = StoreSerialStatus.WrittenOff;
                    unit.WarehouseId = null;
                    await _repository.AddSerialEventAsync(new StoreSerialEvent
                    {
                        StoreSerialUnitId = unit.Id,
                        Type = StoreSerialEventType.WrittenOff,
                        WarehouseId = dto.WarehouseId,
                        DocumentType = "Adjustment",
                        Note = $"{dto.Type}: {dto.Reason.Trim()}",
                        CreatedByUserId = _currentUser.UserId,
                    }, ct);
                }
            }
        }

        await _repository.AddMovementAsync(new StoreStockMovement
        {
            CompanyId = companyId,
            WarehouseId = dto.WarehouseId,
            StoreProductId = product.Id,
            Type = dto.Type,
            Quantity = increase ? dto.Quantity : -dto.Quantity,
            BalanceBefore = before,
            BalanceAfter = balance.Quantity,
            UnitCost = product.AverageCost,
            DocumentType = "Adjustment",
            Reason = dto.Reason.Trim(),
            CreatedByUserId = _currentUser.UserId,
        }, ct);

        try
        {
            await _repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BadRequestException("Stok başqa əməliyyatla dəyişdi. Yenidən cəhd edin.");
        }

        var rows = await _repository.SearchBalancesAsync(companyId, dto.WarehouseId, product.Name, false, ct);
        return Map(rows.First(r => r.StoreProductId == product.Id));
    }

    public async Task<StoreSettingsResponse> Handle(GetStoreSettingsQuery query, CancellationToken ct)
    {
        var settings = await _repository.GetOrCreateSettingsAsync(_currentUser.CompanyId, ct);
        await _repository.SaveChangesAsync(ct);
        return new StoreSettingsResponse
        {
            AllowNegativeStock = settings.AllowNegativeStock,
            MaxDiscountPercent = settings.MaxDiscountPercent,
            BonusEarnPercent = settings.BonusEarnPercent,
            BonusMaxPaymentPercent = settings.BonusMaxPaymentPercent,
            BonusExpiryDays = settings.BonusExpiryDays,
            FiscalEnabled = settings.FiscalEnabled,
            ReceiptWidthMm = settings.ReceiptWidthMm,
            ReceiptHeader = settings.ReceiptHeader,
            ReceiptFooter = settings.ReceiptFooter,
            AllowReceiptlessSale = settings.AllowReceiptlessSale,
            QuickSlotCount = settings.QuickSlotCount,
        };
    }

    public async Task<StoreSettingsResponse> Handle(UpdateStoreSettingsCommand command, CancellationToken ct)
    {
        var settings = await _repository.GetOrCreateSettingsAsync(_currentUser.CompanyId, ct);
        if (command.Request.MaxDiscountPercent is < 0 or > 100)
            throw new BadRequestException("Endirim limiti 0 ilə 100 arasında olmalıdır.");

        settings.AllowNegativeStock = command.Request.AllowNegativeStock;
        if (command.Request.BonusEarnPercent is < 0 or > 100 || command.Request.BonusMaxPaymentPercent is < 0 or > 100)
            throw new BadRequestException("Bonus faizləri 0 ilə 100 arasında olmalıdır.");

        settings.MaxDiscountPercent = command.Request.MaxDiscountPercent;
        settings.BonusEarnPercent = command.Request.BonusEarnPercent;
        settings.BonusMaxPaymentPercent = command.Request.BonusMaxPaymentPercent;
        if (command.Request.BonusExpiryDays < 0)
            throw new BadRequestException("Bonusun müddəti mənfi ola bilməz.");
        settings.BonusExpiryDays = command.Request.BonusExpiryDays;
        settings.FiscalEnabled = command.Request.FiscalEnabled;
        if (command.Request.ReceiptWidthMm is not (58 or 80))
            throw new BadRequestException("Çek eni 58 və ya 80 mm olmalıdır.");
        settings.ReceiptWidthMm = command.Request.ReceiptWidthMm;
        settings.ReceiptHeader = string.IsNullOrWhiteSpace(command.Request.ReceiptHeader) ? null : command.Request.ReceiptHeader.Trim();
        settings.ReceiptFooter = string.IsNullOrWhiteSpace(command.Request.ReceiptFooter) ? null : command.Request.ReceiptFooter.Trim();
        settings.AllowReceiptlessSale = command.Request.AllowReceiptlessSale;
        if (command.Request.QuickSlotCount is < 1 or > 10000)
            throw new BadRequestException("Sürətli düymələrin sayı 1 ilə 10000 arasında olmalıdır.");
        settings.QuickSlotCount = command.Request.QuickSlotCount;
        await _repository.SaveChangesAsync(ct);
        return new StoreSettingsResponse
        {
            AllowNegativeStock = settings.AllowNegativeStock,
            MaxDiscountPercent = settings.MaxDiscountPercent,
            BonusEarnPercent = settings.BonusEarnPercent,
            BonusMaxPaymentPercent = settings.BonusMaxPaymentPercent,
            BonusExpiryDays = settings.BonusExpiryDays,
            FiscalEnabled = settings.FiscalEnabled,
            ReceiptWidthMm = settings.ReceiptWidthMm,
            ReceiptHeader = settings.ReceiptHeader,
            ReceiptFooter = settings.ReceiptFooter,
            AllowReceiptlessSale = settings.AllowReceiptlessSale,
            QuickSlotCount = settings.QuickSlotCount,
        };
    }

    private static StoreStockBalanceResponse Map(StoreStockBalance b)
    {
        var p = b.StoreProduct;
        return new StoreStockBalanceResponse
        {
            WarehouseId = b.WarehouseId,
            WarehouseName = b.Warehouse?.Name ?? string.Empty,
            ProductId = b.StoreProductId,
            ProductName = p.Name,
            Sku = p.Sku,
            PrimaryBarcode = p.Barcodes.OrderByDescending(x => x.IsPrimary).Select(x => x.Barcode).FirstOrDefault(),
            Unit = p.Unit,
            Quantity = b.Quantity,
            MinimumStock = p.MinimumStock,
            IsLow = b.Quantity <= p.MinimumStock,
            AverageCost = p.AverageCost,
            Value = Math.Round(b.Quantity * p.AverageCost, 2, MidpointRounding.AwayFromZero),
        };
    }
}
