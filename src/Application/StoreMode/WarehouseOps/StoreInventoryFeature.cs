using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.StoreMode.Notifications;
using Domain.Constants;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.StoreMode.WarehouseOps;

public record GetStoreInventoriesQuery(int? WarehouseId, StoreInventoryStatus? Status) : IRequest<List<StoreInventoryResponse>>;

public record GetStoreInventoryQuery(int Id) : IRequest<StoreInventoryResponse>;

public record StartStoreInventoryCommand(StartStoreInventoryRequest Request) : IRequest<StoreInventoryResponse>;

public record SaveStoreInventoryCountsCommand(int Id, SaveStoreInventoryCountsRequest Request) : IRequest<StoreInventoryResponse>;

public record ConfirmStoreInventoryCommand(int Id) : IRequest<StoreInventoryResponse>;

public record CancelStoreInventoryCommand(int Id) : IRequest<StoreInventoryResponse>;

public class StoreInventoryHandlers :
    IRequestHandler<GetStoreInventoriesQuery, List<StoreInventoryResponse>>,
    IRequestHandler<GetStoreInventoryQuery, StoreInventoryResponse>,
    IRequestHandler<StartStoreInventoryCommand, StoreInventoryResponse>,
    IRequestHandler<SaveStoreInventoryCountsCommand, StoreInventoryResponse>,
    IRequestHandler<ConfirmStoreInventoryCommand, StoreInventoryResponse>,
    IRequestHandler<CancelStoreInventoryCommand, StoreInventoryResponse>
{
    private readonly IStoreWarehouseOpsRepository _ops;
    private readonly IStoreRepository _store;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StoreNotifier _notifier;

    private readonly Application.StoreMode.Access.StoreAccessService _access;

    public StoreInventoryHandlers(
        IStoreWarehouseOpsRepository ops,
        IStoreRepository store,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        StoreNotifier notifier,
        Application.StoreMode.Access.StoreAccessService access)
    {
        _access = access;
        _notifier = notifier;
        _ops = ops;
        _store = store;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    private static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    public async Task<List<StoreInventoryResponse>> Handle(GetStoreInventoriesQuery query, CancellationToken ct)
    {
        var rows = await _ops.SearchInventoriesAsync(_currentUser.CompanyId, query.WarehouseId, query.Status, ct);
        if (await _access.AllowedWarehousesAsync(ct) is { } allowedWarehouses)
            rows = rows.Where(r => allowedWarehouses.Contains(r.WarehouseId)).ToList();
        var result = new List<StoreInventoryResponse>();
        foreach (var r in rows)
            result.Add(await MapAsync(r, includeLines: false, ct));
        return result;
    }

    public async Task<StoreInventoryResponse> Handle(GetStoreInventoryQuery query, CancellationToken ct)
    {
        var inv = await _ops.GetInventoryAsync(query.Id, _currentUser.CompanyId, ct) ?? throw new NotFoundException("İnventarizasiya tapılmadı.");
        await _access.EnsureWarehouseAsync(inv.WarehouseId, ct);
        return await MapAsync(inv, includeLines: true, ct);
    }

    public async Task<StoreInventoryResponse> Handle(StartStoreInventoryCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;

        await _access.EnsureWarehouseAsync(dto.WarehouseId, ct);
        if (!await _store.WarehouseExistsAsync(companyId, dto.WarehouseId, ct))
            throw new BadRequestException("Anbar tapılmadı.");
        if (dto.CategoryId is { } categoryId && await _store.GetCategoryAsync(categoryId, companyId, ct) is null)
            throw new BadRequestException("Kateqoriya tapılmadı.");
        if (await _ops.OpenInventoryExistsAsync(companyId, dto.WarehouseId, dto.CategoryId, ct))
            throw new BadRequestException("Bu anbar üçün açıq inventarizasiya var; əvvəlcə onu təsdiqləyin və ya ləğv edin.");

        var products = await _ops.GetCountableProductsAsync(companyId, dto.WarehouseId, dto.CategoryId, ct);
        if (products.Count == 0)
            throw new BadRequestException("Sayılacaq məhsul tapılmadı (bu anbarda qalığı olan məhsul yoxdur).");

        var inv = new StoreInventory
        {
            CompanyId = companyId,
            DocumentNo = Guid.NewGuid().ToString("N"),
            WarehouseId = dto.WarehouseId,
            CategoryId = dto.CategoryId,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
            CreatedByUserId = _currentUser.UserId,
            // What the system expects is frozen now; selling during the count does not change it.
            Lines = products.Select(p => new StoreInventoryLine
            {
                StoreProductId = p.Product.Id,
                ExpectedQuantity = p.Quantity,
                UnitCost = p.Product.AverageCost,
            }).ToList(),
        };
        await _ops.AddInventoryAsync(inv, ct);
        await _ops.SaveChangesAsync(ct);
        inv.DocumentNo = $"INV-{inv.Id:D6}";
        await _ops.SaveChangesAsync(ct);

        var saved = await _ops.GetInventoryAsync(inv.Id, companyId, ct) ?? inv;
        return await MapAsync(saved, includeLines: true, ct);
    }

    public async Task<StoreInventoryResponse> Handle(SaveStoreInventoryCountsCommand command, CancellationToken ct)
    {
        var inv = await _ops.GetInventoryAsync(command.Id, _currentUser.CompanyId, ct) ?? throw new NotFoundException("İnventarizasiya tapılmadı.");
        await _access.EnsureWarehouseAsync(inv.WarehouseId, ct);
        if (inv.Status != StoreInventoryStatus.Draft)
            throw new BadRequestException("Bağlanmış inventarizasiyada say dəyişmək olmaz.");

        foreach (var c in command.Request.Lines ?? new())
        {
            var line = inv.Lines.FirstOrDefault(l => l.Id == c.LineId)
                ?? throw new BadRequestException("Sətir bu inventarizasiyaya aid deyil.");
            if (c.CountedQuantity is < 0)
                throw new BadRequestException($"\"{line.StoreProduct.Name}\" üçün sayılan miqdar mənfi ola bilməz.");
            if (c.CountedQuantity is { } q && line.StoreProduct.Unit == UnitOfMeasure.Piece && q != Math.Floor(q))
                throw new BadRequestException($"\"{line.StoreProduct.Name}\" ədədlə sayılır, miqdar tam olmalıdır.");

            line.CountedQuantity = c.CountedQuantity;
            line.Reason = string.IsNullOrWhiteSpace(c.Reason) ? null : c.Reason.Trim();
        }

        await _ops.SaveChangesAsync(ct);
        return await MapAsync(inv, includeLines: true, ct);
    }

    /// <summary>Confirming turns the differences into stock adjustments (counted minus what was expected, applied to the current stock).</summary>
    public async Task<StoreInventoryResponse> Handle(ConfirmStoreInventoryCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;

        var inv = await _ops.GetInventoryAsync(command.Id, companyId, ct) ?? throw new NotFoundException("İnventarizasiya tapılmadı.");
        await _access.EnsureWarehouseAsync(inv.WarehouseId, ct);
        if (inv.Status != StoreInventoryStatus.Draft)
            throw new BadRequestException("Bu inventarizasiya artıq bağlanıb.");
        if (inv.Lines.All(l => l.CountedQuantity is null))
            throw new BadRequestException("Heç bir məhsul sayılmayıb.");

        var missingReason = inv.Lines.FirstOrDefault(l => l.CountedQuantity is { } c && c != l.ExpectedQuantity && string.IsNullOrWhiteSpace(l.Reason));
        if (missingReason is not null)
            throw new BadRequestException($"\"{missingReason.StoreProduct.Name}\" üçün fərqin səbəbi yazılmalıdır.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            foreach (var line in inv.Lines.Where(l => l.CountedQuantity is not null))
            {
                line.Difference = line.CountedQuantity!.Value - line.ExpectedQuantity;
                line.UnitCost = line.StoreProduct.AverageCost > 0 ? line.StoreProduct.AverageCost : line.UnitCost;
                if (line.Difference == 0)
                    continue;

                var balance = await _store.GetOrCreateBalanceAsync(companyId, inv.WarehouseId, line.StoreProductId, ct);
                var before = balance.Quantity;
                balance.Quantity += line.Difference;

                await _store.AddMovementAsync(new StoreStockMovement
                {
                    CompanyId = companyId,
                    WarehouseId = inv.WarehouseId,
                    StoreProductId = line.StoreProductId,
                    Type = line.Difference > 0 ? StoreStockMovementType.AdjustmentIncrease : StoreStockMovementType.AdjustmentDecrease,
                    Quantity = line.Difference,
                    BalanceBefore = before,
                    BalanceAfter = balance.Quantity,
                    UnitCost = line.UnitCost,
                    DocumentType = "Inventory",
                    DocumentId = inv.Id,
                    DocumentNo = inv.DocumentNo,
                    Reason = line.Reason,
                    CreatedByUserId = userId,
                }, ct);
            }

            inv.Status = StoreInventoryStatus.Confirmed;
            inv.ConfirmedAtUtc = DateTime.UtcNow;
            inv.ConfirmedByUserId = userId;
            await _ops.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            var differing = inv.Lines.Count(l => l.Difference != 0);
            if (differing > 0)
                await _notifier.NotifyHoldersAsync(
                    AppPermissions.StoreReportView, "İnventarizasiya fərqi",
                    $"{inv.DocumentNo} ({inv.Warehouse.Name}): {differing} məhsulda fərq tapıldı.",
                    "StoreInventory", inv.Id, "StoreInventory", ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            throw new BadRequestException("Stok başqa əməliyyatla dəyişdi. Yenidən cəhd edin.");
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return await MapAsync(inv, includeLines: true, ct);
    }

    public async Task<StoreInventoryResponse> Handle(CancelStoreInventoryCommand command, CancellationToken ct)
    {
        var inv = await _ops.GetInventoryAsync(command.Id, _currentUser.CompanyId, ct) ?? throw new NotFoundException("İnventarizasiya tapılmadı.");
        await _access.EnsureWarehouseAsync(inv.WarehouseId, ct);
        if (inv.Status != StoreInventoryStatus.Draft)
            throw new BadRequestException("Yalnız açıq inventarizasiyanı ləğv etmək olar.");

        inv.Status = StoreInventoryStatus.Cancelled;
        inv.CancelledAtUtc = DateTime.UtcNow;
        await _ops.SaveChangesAsync(ct);
        return await MapAsync(inv, includeLines: false, ct);
    }

    private async Task<StoreInventoryResponse> MapAsync(StoreInventory inv, bool includeLines, CancellationToken ct)
    {
        var names = await _store.GetUserNamesAsync(new[] { inv.ConfirmedByUserId }.OfType<int>(), ct);
        var confirmed = inv.Status == StoreInventoryStatus.Confirmed;

        // While counting the difference is live; once confirmed it is the fixed figure.
        decimal Diff(StoreInventoryLine l) => confirmed ? l.Difference : (l.CountedQuantity is { } c ? c - l.ExpectedQuantity : 0m);

        var lines = inv.Lines.OrderBy(l => l.StoreProduct.Name).ToList();
        return new StoreInventoryResponse
        {
            Id = inv.Id,
            DocumentNo = inv.DocumentNo,
            WarehouseId = inv.WarehouseId,
            WarehouseName = inv.Warehouse?.Name ?? string.Empty,
            CategoryId = inv.CategoryId,
            CategoryName = inv.Category?.Name,
            Status = inv.Status,
            Note = inv.Note,
            CreatedAtUtc = inv.CreatedAtUtc,
            ConfirmedAtUtc = inv.ConfirmedAtUtc,
            ConfirmedByName = inv.ConfirmedByUserId is { } id && names.TryGetValue(id, out var n) ? n : null,
            LineCount = lines.Count,
            CountedCount = lines.Count(l => l.CountedQuantity is not null),
            DifferenceCount = lines.Count(l => Diff(l) != 0),
            SurplusValue = R2(lines.Where(l => Diff(l) > 0).Sum(l => Diff(l) * l.UnitCost)),
            ShortageValue = R2(lines.Where(l => Diff(l) < 0).Sum(l => Diff(l) * l.UnitCost)),
            Lines = includeLines
                ? lines.Select(l => new StoreInventoryLineResponse
                {
                    Id = l.Id,
                    ProductId = l.StoreProductId,
                    ProductName = l.StoreProduct.Name,
                    Barcode = l.StoreProduct.Barcodes.OrderByDescending(b => b.IsPrimary).Select(b => b.Barcode).FirstOrDefault(),
                    Unit = l.StoreProduct.Unit,
                    ExpectedQuantity = l.ExpectedQuantity,
                    CountedQuantity = l.CountedQuantity,
                    Difference = Diff(l),
                    UnitCost = l.UnitCost,
                    DifferenceValue = R2(Diff(l) * l.UnitCost),
                    Reason = l.Reason,
                }).ToList()
                : new(),
        };
    }
}
