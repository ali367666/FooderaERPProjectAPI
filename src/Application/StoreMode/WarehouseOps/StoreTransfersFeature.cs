using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Application.StoreMode.Notifications;
using Domain.Constants;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.StoreMode.WarehouseOps;

public record GetStoreTransfersQuery(StoreTransferStatus? Status, int? WarehouseId, DateTime? From, DateTime? To)
    : IRequest<List<StoreTransferResponse>>;

public record GetStoreTransferQuery(int Id) : IRequest<StoreTransferResponse>;

public record SaveStoreTransferCommand(int? Id, SaveStoreTransferRequest Request) : IRequest<StoreTransferResponse>;

public record SubmitStoreTransferCommand(int Id) : IRequest<StoreTransferResponse>;

public record ShipStoreTransferCommand(int Id) : IRequest<StoreTransferResponse>;

public record ReceiveStoreTransferCommand(int Id, ReceiveStoreTransferRequest Request) : IRequest<StoreTransferResponse>;

public record CancelStoreTransferCommand(int Id) : IRequest<StoreTransferResponse>;

public class StoreTransfersHandlers :
    IRequestHandler<GetStoreTransfersQuery, List<StoreTransferResponse>>,
    IRequestHandler<GetStoreTransferQuery, StoreTransferResponse>,
    IRequestHandler<SaveStoreTransferCommand, StoreTransferResponse>,
    IRequestHandler<SubmitStoreTransferCommand, StoreTransferResponse>,
    IRequestHandler<ShipStoreTransferCommand, StoreTransferResponse>,
    IRequestHandler<ReceiveStoreTransferCommand, StoreTransferResponse>,
    IRequestHandler<CancelStoreTransferCommand, StoreTransferResponse>
{
    private static readonly char[] Sep = { ',' };

    private readonly IStoreWarehouseOpsRepository _ops;
    private readonly IStoreRepository _store;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StoreNotifier _notifier;

    private readonly Application.StoreMode.Access.StoreAccessService _access;

    public StoreTransfersHandlers(
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

    private static List<string> Split(string? s)
        => (s ?? string.Empty).Split(Sep, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    // ---- Read -------------------------------------------------------------------------------

    public async Task<List<StoreTransferResponse>> Handle(GetStoreTransfersQuery query, CancellationToken ct)
    {
        DateTime? fromUtc = query.From is { } f ? BusinessTime.ToUtc(f.Date + ReportPeriodResolver.DayStart) : null;
        DateTime? toUtc = query.To is { } t ? BusinessTime.ToUtc(t.Date.AddDays(1) + ReportPeriodResolver.DayStart) : null;

        var rows = await _ops.SearchTransfersAsync(_currentUser.CompanyId, query.Status, query.WarehouseId, fromUtc, toUtc, ct);
        if (await _access.AllowedWarehousesAsync(ct) is { } allowedWarehouses)
            rows = rows.Where(r => allowedWarehouses.Contains(r.FromWarehouseId) || allowedWarehouses.Contains(r.ToWarehouseId)).ToList();
        var result = new List<StoreTransferResponse>();
        foreach (var r in rows)
            result.Add(await MapAsync(r, false, ct));
        return result;
    }

    public async Task<StoreTransferResponse> Handle(GetStoreTransferQuery query, CancellationToken ct)
    {
        var transfer = await _ops.GetTransferAsync(query.Id, _currentUser.CompanyId, ct) ?? throw new NotFoundException("Transfer tapılmadı.");
        await _access.EnsureAnyWarehouseAsync(ct, transfer.FromWarehouseId, transfer.ToWarehouseId);
        return await MapAsync(transfer, true, ct);
    }

    // ---- Draft ------------------------------------------------------------------------------

    public async Task<StoreTransferResponse> Handle(SaveStoreTransferCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;

        await _access.EnsureAnyWarehouseAsync(ct, dto.FromWarehouseId, dto.ToWarehouseId);
        if (dto.FromWarehouseId == dto.ToWarehouseId)
            throw new BadRequestException("Göndərən və qəbul edən anbar eyni ola bilməz.");
        if (!await _store.WarehouseExistsAsync(companyId, dto.FromWarehouseId, ct) || !await _store.WarehouseExistsAsync(companyId, dto.ToWarehouseId, ct))
            throw new BadRequestException("Anbar tapılmadı.");
        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new BadRequestException("Transfer ən azı bir sətir içərməlidir.");
        if (dto.Lines.GroupBy(l => l.ProductId).Any(g => g.Count() > 1))
            throw new BadRequestException("Eyni məhsul iki sətirdə yazılıb.");

        var products = (await _store.GetProductsByIdsAsync(companyId, dto.Lines.Select(l => l.ProductId).ToList(), ct)).ToDictionary(p => p.Id);
        var lines = new List<StoreTransferLine>();
        foreach (var l in dto.Lines)
        {
            if (!products.TryGetValue(l.ProductId, out var product))
                throw new BadRequestException("Sətirdəki məhsullardan biri tapılmadı.");
            if (l.Quantity <= 0)
                throw new BadRequestException($"\"{product.Name}\" üçün miqdar 0-dan böyük olmalıdır.");
            if (product.Unit == UnitOfMeasure.Piece && l.Quantity != Math.Floor(l.Quantity))
                throw new BadRequestException($"\"{product.Name}\" ədədlə köçürülür, miqdar tam olmalıdır.");

            var serials = (l.SerialNumbers ?? new()).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            lines.Add(new StoreTransferLine
            {
                StoreProductId = product.Id,
                Quantity = l.Quantity,
                SerialNumbers = serials.Count == 0 ? null : string.Join(",", serials),
            });
        }

        StoreTransfer transfer;
        if (command.Id is { } id)
        {
            transfer = await _ops.GetTransferAsync(id, companyId, ct) ?? throw new NotFoundException("Transfer tapılmadı.");
            if (transfer.Status is not (StoreTransferStatus.Draft or StoreTransferStatus.Pending))
                throw new BadRequestException("Göndərilmiş transferi dəyişmək olmaz.");
            foreach (var old in transfer.Lines.ToList())
                _ops.RemoveTransferLine(old);
            transfer.Lines.Clear();
        }
        else
        {
            transfer = new StoreTransfer { CompanyId = companyId, DocumentNo = Guid.NewGuid().ToString("N"), CreatedByUserId = _currentUser.UserId };
            await _ops.AddTransferAsync(transfer, ct);
        }

        transfer.FromWarehouseId = dto.FromWarehouseId;
        transfer.ToWarehouseId = dto.ToWarehouseId;
        transfer.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
        transfer.Status = StoreTransferStatus.Draft;
        foreach (var line in lines)
            transfer.Lines.Add(line);

        await _ops.SaveChangesAsync(ct);
        if (command.Id is null)
        {
            transfer.DocumentNo = $"TR-{transfer.Id:D6}";
            await _ops.SaveChangesAsync(ct);
        }

        var saved = await _ops.GetTransferAsync(transfer.Id, companyId, ct) ?? transfer;
        return await MapAsync(saved, true, ct);
    }

    public async Task<StoreTransferResponse> Handle(SubmitStoreTransferCommand command, CancellationToken ct)
    {
        var transfer = await _ops.GetTransferAsync(command.Id, _currentUser.CompanyId, ct) ?? throw new NotFoundException("Transfer tapılmadı.");
        await _access.EnsureAnyWarehouseAsync(ct, transfer.FromWarehouseId, transfer.ToWarehouseId);
        if (transfer.Status != StoreTransferStatus.Draft)
            throw new BadRequestException("Yalnız qaralama transferi göndərişə təqdim etmək olar.");
        if (transfer.Lines.Count == 0)
            throw new BadRequestException("Transferin sətri yoxdur.");

        transfer.Status = StoreTransferStatus.Pending;
        await _ops.SaveChangesAsync(ct);
        return await MapAsync(transfer, true, ct);
    }

    public async Task<StoreTransferResponse> Handle(CancelStoreTransferCommand command, CancellationToken ct)
    {
        var transfer = await _ops.GetTransferAsync(command.Id, _currentUser.CompanyId, ct) ?? throw new NotFoundException("Transfer tapılmadı.");
        await _access.EnsureAnyWarehouseAsync(ct, transfer.FromWarehouseId, transfer.ToWarehouseId);
        if (transfer.Status is not (StoreTransferStatus.Draft or StoreTransferStatus.Pending))
            throw new BadRequestException("Göndərilmiş transferi ləğv etmək olmaz; qəbul edin və ya çatışmazlıqla bağlayın.");

        transfer.Status = StoreTransferStatus.Cancelled;
        transfer.CancelledAtUtc = DateTime.UtcNow;
        await _ops.SaveChangesAsync(ct);
        return await MapAsync(transfer, true, ct);
    }

    // ---- Ship -------------------------------------------------------------------------------

    /// <summary>Shipping takes the goods out of the source warehouse; they are in transit until received.</summary>
    public async Task<StoreTransferResponse> Handle(ShipStoreTransferCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;

        var transfer = await _ops.GetTransferAsync(command.Id, companyId, ct) ?? throw new NotFoundException("Transfer tapılmadı.");
        await _access.EnsureAnyWarehouseAsync(ct, transfer.FromWarehouseId, transfer.ToWarehouseId);
        if (transfer.Status is not (StoreTransferStatus.Draft or StoreTransferStatus.Pending))
            throw new BadRequestException("Bu transfer artıq göndərilib və ya ləğv edilib.");
        if (transfer.Lines.Count == 0)
            throw new BadRequestException("Transferin sətri yoxdur.");

        var serialByLine = new Dictionary<int, List<StoreSerialUnit>>();
        foreach (var line in transfer.Lines.Where(l => l.StoreProduct.TracksSerial))
        {
            var serials = Split(line.SerialNumbers);
            if (line.Quantity != Math.Floor(line.Quantity) || serials.Count != (int)line.Quantity)
                throw new BadRequestException($"\"{line.StoreProduct.Name}\" üçün {line.Quantity:0.##} ədəd IMEI/serial seçilməlidir (seçilib: {serials.Count}).");
            var units = await _store.FindSerialUnitsAsync(companyId, serials, ct);
            foreach (var serial in serials)
            {
                var unit = units.FirstOrDefault(u => u.SerialNumber == serial);
                if (unit is null || unit.StoreProductId != line.StoreProductId
                    || unit.Status != StoreSerialStatus.InStock || unit.WarehouseId != transfer.FromWarehouseId)
                    throw new BadRequestException($"\"{serial}\" göndərən anbarda stokda deyil.");
            }
            serialByLine[line.Id] = units;
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            foreach (var line in transfer.Lines)
            {
                var balance = await _store.GetOrCreateBalanceAsync(companyId, transfer.FromWarehouseId, line.StoreProductId, ct);
                if (balance.Quantity < line.Quantity)
                    throw new BadRequestException(
                        $"\"{line.StoreProduct.Name}\" göndərən anbarda kifayət qədər yoxdur (lazımdır: {line.Quantity}, var: {balance.Quantity}).");

                var before = balance.Quantity;
                balance.Quantity -= line.Quantity;
                line.ShippedQuantity = line.Quantity;
                line.UnitCost = line.StoreProduct.AverageCost > 0 ? line.StoreProduct.AverageCost : line.StoreProduct.PurchasePrice;

                await _store.AddMovementAsync(new StoreStockMovement
                {
                    CompanyId = companyId,
                    WarehouseId = transfer.FromWarehouseId,
                    StoreProductId = line.StoreProductId,
                    Type = StoreStockMovementType.TransferOut,
                    Quantity = -line.Quantity,
                    BalanceBefore = before,
                    BalanceAfter = balance.Quantity,
                    UnitCost = line.UnitCost,
                    DocumentType = "Transfer",
                    DocumentId = transfer.Id,
                    DocumentNo = transfer.DocumentNo,
                    Reason = $"→ {transfer.ToWarehouse.Name}",
                    CreatedByUserId = userId,
                }, ct);

                if (serialByLine.TryGetValue(line.Id, out var units))
                    foreach (var unit in units)
                    {
                        unit.Status = StoreSerialStatus.InTransit;
                        unit.WarehouseId = null;
                        await _store.AddSerialEventAsync(new StoreSerialEvent
                        {
                            StoreSerialUnitId = unit.Id,
                            Type = StoreSerialEventType.TransferOut,
                            WarehouseId = transfer.FromWarehouseId,
                            DocumentType = "Transfer",
                            DocumentId = transfer.Id,
                            DocumentNo = transfer.DocumentNo,
                            Note = $"→ {transfer.ToWarehouse.Name}",
                            CreatedByUserId = userId,
                        }, ct);
                    }
            }

            transfer.Status = StoreTransferStatus.Shipped;
            transfer.ShippedAtUtc = DateTime.UtcNow;
            transfer.ShippedByUserId = userId;
            await _ops.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            await _notifier.NotifyHoldersAsync(
                AppPermissions.StoreTransferReceive, "Transfer göndərildi",
                $"{transfer.DocumentNo}: {transfer.FromWarehouse.Name} → {transfer.ToWarehouse.Name} ({transfer.Lines.Count} sətir) yoldadır.",
                "StoreTransfer", transfer.Id, "StoreTransfer", ct);
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

        return await MapAsync(transfer, true, ct);
    }

    // ---- Receive ----------------------------------------------------------------------------

    /// <summary>Receiving writes the quantity that really arrived into the destination warehouse; the rest stays in transit or is closed as a shortage.</summary>
    public async Task<StoreTransferResponse> Handle(ReceiveStoreTransferCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;
        var dto = command.Request;

        var transfer = await _ops.GetTransferAsync(command.Id, companyId, ct) ?? throw new NotFoundException("Transfer tapılmadı.");
        await _access.EnsureAnyWarehouseAsync(ct, transfer.FromWarehouseId, transfer.ToWarehouseId);
        if (transfer.Status is not (StoreTransferStatus.Shipped or StoreTransferStatus.PartiallyReceived))
            throw new BadRequestException("Yalnız göndərilmiş transferi qəbul etmək olar.");

        var received = new List<(StoreTransferLine Line, decimal Quantity, List<string> Serials)>();
        foreach (var r in (dto.Lines ?? new()).Where(l => l.Quantity > 0 || (l.SerialNumbers?.Count ?? 0) > 0))
        {
            var line = transfer.Lines.FirstOrDefault(l => l.Id == r.LineId) ?? throw new BadRequestException("Sətir bu transferə aid deyil.");
            var serials = (r.SerialNumbers ?? new()).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            var quantity = line.StoreProduct.TracksSerial ? serials.Count : r.Quantity;

            if (quantity <= 0)
                continue;
            var open = line.ShippedQuantity - line.ReceivedQuantity;
            if (quantity > open)
                throw new BadRequestException($"\"{line.StoreProduct.Name}\" üçün ən çox {open} qəbul etmək olar.");
            if (line.StoreProduct.Unit == UnitOfMeasure.Piece && quantity != Math.Floor(quantity))
                throw new BadRequestException($"\"{line.StoreProduct.Name}\" ədədlə qəbul edilir, miqdar tam olmalıdır.");

            if (line.StoreProduct.TracksSerial)
            {
                var sent = Split(line.SerialNumbers);
                var already = Split(line.ReceivedSerialNumbers);
                foreach (var s in serials)
                    if (!sent.Contains(s) || already.Contains(s))
                        throw new BadRequestException($"\"{s}\" bu transferdə göndərilməyib və ya artıq qəbul edilib.");
            }

            received.Add((line, quantity, serials));
        }

        if (received.Count == 0 && !dto.CloseWithShortage)
            throw new BadRequestException("Qəbul edilən miqdar yazılmayıb.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            foreach (var (line, quantity, serials) in received)
            {
                var balance = await _store.GetOrCreateBalanceAsync(companyId, transfer.ToWarehouseId, line.StoreProductId, ct);
                var before = balance.Quantity;
                balance.Quantity += quantity;
                line.ReceivedQuantity += quantity;

                await _store.AddMovementAsync(new StoreStockMovement
                {
                    CompanyId = companyId,
                    WarehouseId = transfer.ToWarehouseId,
                    StoreProductId = line.StoreProductId,
                    Type = StoreStockMovementType.TransferIn,
                    Quantity = quantity,
                    BalanceBefore = before,
                    BalanceAfter = balance.Quantity,
                    UnitCost = line.UnitCost,
                    DocumentType = "Transfer",
                    DocumentId = transfer.Id,
                    DocumentNo = transfer.DocumentNo,
                    Reason = $"← {transfer.FromWarehouse.Name}",
                    CreatedByUserId = userId,
                }, ct);

                if (line.StoreProduct.TracksSerial)
                {
                    line.ReceivedSerialNumbers = string.Join(",", Split(line.ReceivedSerialNumbers).Concat(serials));
                    foreach (var unit in await _store.FindSerialUnitsAsync(companyId, serials, ct))
                    {
                        unit.Status = StoreSerialStatus.InStock;
                        unit.WarehouseId = transfer.ToWarehouseId;
                        await _store.AddSerialEventAsync(new StoreSerialEvent
                        {
                            StoreSerialUnitId = unit.Id,
                            Type = StoreSerialEventType.TransferIn,
                            WarehouseId = transfer.ToWarehouseId,
                            DocumentType = "Transfer",
                            DocumentId = transfer.Id,
                            DocumentNo = transfer.DocumentNo,
                            CreatedByUserId = userId,
                        }, ct);
                    }
                }
            }

            var allIn = transfer.Lines.All(l => l.ReceivedQuantity >= l.ShippedQuantity);
            if (!allIn && dto.CloseWithShortage)
            {
                // What never arrived is noted as a shortage and the transfer is closed.
                foreach (var line in transfer.Lines.Where(l => l.ReceivedQuantity < l.ShippedQuantity))
                {
                    line.ShortageQuantity = line.ShippedQuantity - line.ReceivedQuantity;
                    if (line.StoreProduct.TracksSerial)
                    {
                        var missing = Split(line.SerialNumbers).Except(Split(line.ReceivedSerialNumbers)).ToList();
                        foreach (var unit in await _store.FindSerialUnitsAsync(companyId, missing, ct))
                        {
                            unit.Status = StoreSerialStatus.WrittenOff;
                            await _store.AddSerialEventAsync(new StoreSerialEvent
                            {
                                StoreSerialUnitId = unit.Id,
                                Type = StoreSerialEventType.WrittenOff,
                                DocumentType = "Transfer",
                                DocumentId = transfer.Id,
                                DocumentNo = transfer.DocumentNo,
                                Note = "Transfer çatışmazlığı",
                                CreatedByUserId = userId,
                            }, ct);
                        }
                    }
                }
            }

            if (allIn || dto.CloseWithShortage)
            {
                transfer.Status = StoreTransferStatus.Received;
                transfer.ReceivedAtUtc = DateTime.UtcNow;
                transfer.ReceivedByUserId = userId;
            }
            else
            {
                transfer.Status = StoreTransferStatus.PartiallyReceived;
            }

            await _ops.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            await _notifier.NotifyHoldersAsync(
                AppPermissions.StoreTransferShip, "Transfer qəbul edildi",
                $"{transfer.DocumentNo}: {transfer.ToWarehouse.Name} anbarı qəbul etdi" +
                (transfer.Lines.Any(l => l.ShortageQuantity > 0) ? " (çatışmazlıqla)." : transfer.Status == StoreTransferStatus.PartiallyReceived ? " (qismən)." : "."),
                "StoreTransfer", transfer.Id, "StoreTransfer", ct);
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

        return await MapAsync(transfer, true, ct);
    }

    // ---- Mapping ----------------------------------------------------------------------------

    private async Task<StoreTransferResponse> MapAsync(StoreTransfer t, bool includeLines, CancellationToken ct)
    {
        var names = await _store.GetUserNamesAsync(
            new[] { t.CreatedByUserId, t.ShippedByUserId, t.ReceivedByUserId }.OfType<int>(), ct);
        string? Name(int? id) => id is { } v && names.TryGetValue(v, out var n) ? n : null;

        var open = t.Status is StoreTransferStatus.Shipped or StoreTransferStatus.PartiallyReceived;
        decimal InTransit(StoreTransferLine l) => open ? Math.Max(0, l.ShippedQuantity - l.ReceivedQuantity) : 0m;

        return new StoreTransferResponse
        {
            Id = t.Id,
            DocumentNo = t.DocumentNo,
            FromWarehouseId = t.FromWarehouseId,
            FromWarehouseName = t.FromWarehouse?.Name ?? string.Empty,
            ToWarehouseId = t.ToWarehouseId,
            ToWarehouseName = t.ToWarehouse?.Name ?? string.Empty,
            Status = t.Status,
            Note = t.Note,
            CreatedAtUtc = t.CreatedAtUtc,
            CreatedByName = Name(t.CreatedByUserId),
            ShippedAtUtc = t.ShippedAtUtc,
            ShippedByName = Name(t.ShippedByUserId),
            ReceivedAtUtc = t.ReceivedAtUtc,
            ReceivedByName = Name(t.ReceivedByUserId),
            LineCount = t.Lines.Count,
            InTransitValue = Math.Round(t.Lines.Sum(l => InTransit(l) * l.UnitCost), 2, MidpointRounding.AwayFromZero),
            Lines = includeLines
                ? t.Lines.OrderBy(l => l.Id).Select(l => new StoreTransferLineResponse
                {
                    Id = l.Id,
                    ProductId = l.StoreProductId,
                    ProductName = l.StoreProduct?.Name ?? string.Empty,
                    TracksSerial = l.StoreProduct?.TracksSerial ?? false,
                    Quantity = l.Quantity,
                    ShippedQuantity = l.ShippedQuantity,
                    ReceivedQuantity = l.ReceivedQuantity,
                    ShortageQuantity = l.ShortageQuantity,
                    InTransitQuantity = InTransit(l),
                    SerialNumbers = Split(l.SerialNumbers),
                    ReceivedSerialNumbers = Split(l.ReceivedSerialNumbers),
                }).ToList()
                : new(),
        };
    }
}
