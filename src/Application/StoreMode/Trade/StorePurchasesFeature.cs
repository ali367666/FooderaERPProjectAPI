using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.StoreMode.Trade;

public record SaveStorePurchaseCommand(int? Id, SaveStorePurchaseRequest Request) : IRequest<StorePurchaseResponse>;

public record GetStorePurchaseQuery(int Id) : IRequest<StorePurchaseResponse>;

public record SearchStorePurchasesQuery(
    int? SupplierId, StorePurchaseStatus? Status, DateTime? From, DateTime? To, string? Term, int Page, int PageSize)
    : IRequest<StorePurchasePageResponse>;

public record ConfirmStorePurchaseCommand(int Id, ConfirmStorePurchaseRequest Request) : IRequest<StorePurchaseResponse>;

public record CancelStorePurchaseCommand(int Id, CancelStorePurchaseRequest Request) : IRequest<StorePurchaseResponse>;

public record ReturnStorePurchaseCommand(int Id, ReturnStorePurchaseRequest Request) : IRequest<StorePurchaseResponse>;

public class StorePurchasesHandlers :
    IRequestHandler<SaveStorePurchaseCommand, StorePurchaseResponse>,
    IRequestHandler<GetStorePurchaseQuery, StorePurchaseResponse>,
    IRequestHandler<SearchStorePurchasesQuery, StorePurchasePageResponse>,
    IRequestHandler<ConfirmStorePurchaseCommand, StorePurchaseResponse>,
    IRequestHandler<CancelStorePurchaseCommand, StorePurchaseResponse>,
    IRequestHandler<ReturnStorePurchaseCommand, StorePurchaseResponse>
{
    private static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private readonly IStoreTradeRepository _trade;
    private readonly IStoreRepository _store;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StorePaymentService _payments;

    private readonly Application.StoreMode.Access.StoreAccessService _access;

    public StorePurchasesHandlers(
        IStoreTradeRepository trade,
        IStoreRepository store,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        StorePaymentService payments,
        Application.StoreMode.Access.StoreAccessService access)
    {
        _access = access;
        _trade = trade;
        _store = store;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _payments = payments;
    }

    // ---- Draft ------------------------------------------------------------------------------

    public async Task<StorePurchaseResponse> Handle(SaveStorePurchaseCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;

        var supplier = await _trade.GetSupplierAsync(dto.SupplierId, companyId, ct)
            ?? throw new BadRequestException("Təchizatçı tapılmadı.");
        if (!supplier.IsActive)
            throw new BadRequestException("Bu təchizatçı aktiv deyil.");
        await _access.EnsureWarehouseAsync(dto.WarehouseId, ct);
        if (!await _store.WarehouseExistsAsync(companyId, dto.WarehouseId, ct))
            throw new BadRequestException("Anbar tapılmadı.");
        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new BadRequestException("Alış ən azı bir sətir içərməlidir.");

        var products = (await _store.GetProductsByIdsAsync(companyId, dto.Lines.Select(l => l.ProductId).Distinct().ToList(), ct))
            .ToDictionary(p => p.Id);

        var lines = new List<StorePurchaseLine>();
        foreach (var l in dto.Lines)
        {
            if (!products.TryGetValue(l.ProductId, out var product))
                throw new BadRequestException("Sətirlərdəki məhsullardan biri tapılmadı.");
            if (l.Quantity <= 0)
                throw new BadRequestException($"\"{product.Name}\" üçün miqdar 0-dan böyük olmalıdır.");
            if (product.Unit == UnitOfMeasure.Piece && l.Quantity != Math.Floor(l.Quantity))
                throw new BadRequestException($"\"{product.Name}\" ədədlə alınır, miqdar tam olmalıdır.");
            if (l.UnitPrice < 0)
                throw new BadRequestException($"\"{product.Name}\" üçün qiymət mənfi ola bilməz.");
            if (l.DiscountPercent is < 0 or > 100)
                throw new BadRequestException($"\"{product.Name}\" üçün endirim faizi 0 ilə 100 arasında olmalıdır.");
            var vat = l.VatPercent ?? product.VatPercent ?? 0m;
            if (vat is < 0 or > 100)
                throw new BadRequestException($"\"{product.Name}\" üçün ƏDV faizi 0 ilə 100 arasında olmalıdır.");

            // Prices are as invoiced, before VAT; the line total is what is owed, VAT added.
            var netLine = R2(l.Quantity * l.UnitPrice * (1 - l.DiscountPercent / 100m));
            var serials = (l.SerialNumbers ?? new()).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            lines.Add(new StorePurchaseLine
            {
                StoreProductId = product.Id,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                VatPercent = vat,
                LineTotal = R2(netLine * (1 + vat / 100m)),
                NetUnitCost = Math.Round(l.UnitPrice * (1 - l.DiscountPercent / 100m), 4, MidpointRounding.AwayFromZero),
                SerialNumbers = serials.Count == 0 ? null : string.Join(",", serials),
            });
        }

        StorePurchase purchase;
        if (command.Id is { } id)
        {
            purchase = await _trade.GetPurchaseAsync(id, companyId, ct) ?? throw new NotFoundException("Alış tapılmadı.");
            if (purchase.Status != StorePurchaseStatus.Draft)
                throw new BadRequestException("Yalnız qaralama (draft) alışı dəyişmək olar.");
            foreach (var old in purchase.Lines.ToList())
                _trade.RemovePurchaseLine(old);
            purchase.Lines.Clear();
        }
        else
        {
            purchase = new StorePurchase
            {
                CompanyId = companyId,
                DocumentNo = Guid.NewGuid().ToString("N"),
                CreatedByUserId = _currentUser.UserId,
            };
            await _trade.AddPurchaseAsync(purchase, ct);
        }

        purchase.StoreSupplierId = supplier.Id;
        purchase.WarehouseId = dto.WarehouseId;
        purchase.SupplierInvoiceNo = string.IsNullOrWhiteSpace(dto.SupplierInvoiceNo) ? null : dto.SupplierInvoiceNo.Trim();
        purchase.PurchaseDate = dto.PurchaseDate ?? DateTime.UtcNow;
        purchase.DueDate = dto.DueDate ?? purchase.PurchaseDate.AddDays(supplier.PaymentTermDays);
        purchase.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
        foreach (var line in lines)
            purchase.Lines.Add(line);
        purchase.Total = lines.Sum(l => l.LineTotal);
        purchase.VatTotal = lines.Sum(l => l.LineTotal - R2(l.Quantity * l.UnitPrice * (1 - l.DiscountPercent / 100m)));

        await _trade.SaveChangesAsync(ct);
        if (command.Id is null)
        {
            purchase.DocumentNo = $"P-{purchase.Id:D7}";
            await _trade.SaveChangesAsync(ct);
        }

        var saved = await _trade.GetPurchaseAsync(purchase.Id, companyId, ct) ?? purchase;
        return await MapAsync(saved, ct);
    }

    public async Task<StorePurchaseResponse> Handle(GetStorePurchaseQuery query, CancellationToken ct)
    {
        var purchase = await _trade.GetPurchaseAsync(query.Id, _currentUser.CompanyId, ct)
            ?? throw new NotFoundException("Alış tapılmadı.");
        await _access.EnsureWarehouseAsync(purchase.WarehouseId, ct);
        return await MapAsync(purchase, ct);
    }

    public async Task<StorePurchasePageResponse> Handle(SearchStorePurchasesQuery query, CancellationToken ct)
    {
        DateTime? fromUtc = query.From is { } f ? BusinessTime.ToUtc(f.Date + ReportPeriodResolver.DayStart) : null;
        DateTime? toUtc = query.To is { } t ? BusinessTime.ToUtc(t.Date.AddDays(1) + ReportPeriodResolver.DayStart) : null;
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var page = Math.Max(1, query.Page);

        var (items, total) = await _trade.SearchPurchasesAsync(
            _currentUser.CompanyId, query.SupplierId, query.Status, fromUtc, toUtc, query.Term, (page - 1) * pageSize, pageSize, ct,
            await _access.AllowedWarehousesAsync(ct));
        var ids = items.Select(i => i.Id).ToList();
        var paid = await _trade.GetPaidByPurchaseAsync(ids, ct);
        var returned = await _trade.GetReturnedAmountByPurchaseAsync(ids, ct);

        return new StorePurchasePageResponse
        {
            Total = total,
            Items = items.Select(p => new StorePurchaseListItemResponse
            {
                Id = p.Id,
                DocumentNo = p.DocumentNo,
                SupplierName = p.StoreSupplier?.Name ?? string.Empty,
                SupplierInvoiceNo = p.SupplierInvoiceNo,
                PurchaseDate = p.PurchaseDate,
                Status = p.Status,
                Total = p.Total,
                Paid = paid.GetValueOrDefault(p.Id),
                Outstanding = p.Status == StorePurchaseStatus.Confirmed ? StorePaymentService.Outstanding(p, paid, returned) : 0m,
            }).ToList(),
        };
    }

    // ---- Confirm ----------------------------------------------------------------------------

    public async Task<StorePurchaseResponse> Handle(ConfirmStorePurchaseCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;

        var purchase = await _trade.GetPurchaseAsync(command.Id, companyId, ct) ?? throw new NotFoundException("Alış tapılmadı.");
        await _access.EnsureWarehouseAsync(purchase.WarehouseId, ct);
        if (purchase.Status != StorePurchaseStatus.Draft)
            throw new BadRequestException("Yalnız qaralama alışı təsdiq etmək olar.");
        if (purchase.Lines.Count == 0)
            throw new BadRequestException("Alışın sətri yoxdur.");

        var paymentRequest = command.Request?.Payment;
        if (paymentRequest is not null && paymentRequest.Amount > purchase.Total)
            throw new BadRequestException("Ödəniş alışın məbləğindən çox ola bilməz.");

        // IMEI / serial units must be complete and new.
        var serialByLine = new Dictionary<int, List<string>>();
        foreach (var line in purchase.Lines.Where(l => l.StoreProduct.TracksSerial))
        {
            var serials = (line.SerialNumbers ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            if (line.Quantity != Math.Floor(line.Quantity) || serials.Count != (int)line.Quantity)
                throw new BadRequestException($"\"{line.StoreProduct.Name}\" üçün {line.Quantity:0.##} ədəd IMEI/serial nömrəsi yazılmalıdır (yazılıb: {serials.Count}).");
            serialByLine[line.Id] = serials;
        }
        var allSerials = serialByLine.Values.SelectMany(s => s).ToList();
        if (allSerials.Count != allSerials.Distinct().Count())
            throw new BadRequestException("Eyni IMEI/serial nömrəsi iki dəfə yazılıb.");
        if (allSerials.Count > 0 && (await _store.FindSerialUnitsAsync(companyId, allSerials, ct)) is { Count: > 0 } clash)
            throw new BadRequestException($"\"{clash[0].SerialNumber}\" nömrəsi artıq sistemdə var.");

        var supplier = purchase.StoreSupplier;
        var now = DateTime.UtcNow;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            foreach (var group in purchase.Lines.GroupBy(l => l.StoreProductId))
            {
                var product = group.First().StoreProduct;
                var quantity = group.Sum(l => l.Quantity);
                var cost = group.Sum(l => l.Quantity * l.NetUnitCost);

                // Weighted average cost over everything in stock; with nothing on hand it is this purchase's price.
                var onHand = Math.Max(0, await _store.GetTotalQuantityAsync(companyId, product.Id, ct));
                var oldPurchasePrice = product.PurchasePrice;
                product.AverageCost = onHand > 0 && product.AverageCost > 0
                    ? Math.Round((onHand * product.AverageCost + cost) / (onHand + quantity), 4, MidpointRounding.AwayFromZero)
                    : Math.Round(cost / quantity, 4, MidpointRounding.AwayFromZero);

                var lastCost = group.Last().NetUnitCost;
                if (lastCost != oldPurchasePrice)
                {
                    product.PurchasePrice = R2(lastCost);
                    await _trade.AddPriceHistoryAsync(new StorePriceHistory
                    {
                        CompanyId = companyId,
                        StoreProductId = product.Id,
                        PriceType = StorePriceType.Purchase,
                        OldPrice = oldPurchasePrice,
                        NewPrice = product.PurchasePrice,
                        Reason = $"Alış {purchase.DocumentNo}",
                        CreatedByUserId = userId,
                    }, ct);
                }

                var balance = await _store.GetOrCreateBalanceAsync(companyId, purchase.WarehouseId, product.Id, ct);
                var before = balance.Quantity;
                balance.Quantity += quantity;
                await _store.AddMovementAsync(new StoreStockMovement
                {
                    CompanyId = companyId,
                    WarehouseId = purchase.WarehouseId,
                    StoreProductId = product.Id,
                    Type = StoreStockMovementType.Purchase,
                    Quantity = quantity,
                    BalanceBefore = before,
                    BalanceAfter = balance.Quantity,
                    UnitCost = Math.Round(cost / quantity, 4, MidpointRounding.AwayFromZero),
                    DocumentType = "Purchase",
                    DocumentId = purchase.Id,
                    DocumentNo = purchase.DocumentNo,
                    CreatedByUserId = userId,
                }, ct);
            }

            foreach (var (lineId, serials) in serialByLine)
            {
                var line = purchase.Lines.First(l => l.Id == lineId);
                foreach (var serial in serials)
                {
                    var unit = new StoreSerialUnit
                    {
                        CompanyId = companyId,
                        StoreProductId = line.StoreProductId,
                        SerialNumber = serial,
                        WarehouseId = purchase.WarehouseId,
                        Status = StoreSerialStatus.InStock,
                        CreatedByUserId = userId,
                    };
                    unit.Events.Add(new StoreSerialEvent
                    {
                        Type = StoreSerialEventType.Received,
                        WarehouseId = purchase.WarehouseId,
                        DocumentType = "Purchase",
                        DocumentId = purchase.Id,
                        DocumentNo = purchase.DocumentNo,
                        CreatedByUserId = userId,
                    });
                    await _store.AddSerialUnitAsync(unit, ct);
                }
            }

            purchase.Status = StorePurchaseStatus.Confirmed;
            purchase.ConfirmedAtUtc = now;
            purchase.ConfirmedByUserId = userId;

            await _trade.AddLedgerEntryAsync(new StoreLedgerEntry
            {
                CompanyId = companyId,
                PartyType = StoreLedgerPartyType.Supplier,
                PartyId = supplier.Id,
                Type = StoreLedgerEntryType.Purchase,
                Amount = purchase.Total,
                EntryDate = purchase.PurchaseDate,
                DocumentType = "Purchase",
                DocumentId = purchase.Id,
                DocumentNo = purchase.DocumentNo,
                Note = purchase.SupplierInvoiceNo,
                CreatedByUserId = userId,
            }, ct);
            await _trade.SaveChangesAsync(ct);

            if (paymentRequest is { Amount: > 0 })
            {
                await _payments.CreateAsync(
                    supplier, paymentRequest.Amount, paymentRequest.Method, null, paymentRequest.Reference,
                    $"Alış {purchase.DocumentNo} üçün", null, purchase.Id, ct);
                await _trade.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);
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

        return await MapAsync(purchase, ct);
    }

    // ---- Cancel -----------------------------------------------------------------------------

    public async Task<StorePurchaseResponse> Handle(CancelStorePurchaseCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;
        var reason = command.Request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
            throw new BadRequestException("Ləğv səbəbi yazılmalıdır.");

        var purchase = await _trade.GetPurchaseAsync(command.Id, companyId, ct) ?? throw new NotFoundException("Alış tapılmadı.");
        await _access.EnsureWarehouseAsync(purchase.WarehouseId, ct);
        if (purchase.Status == StorePurchaseStatus.Cancelled)
            throw new BadRequestException("Bu alış artıq ləğv edilib.");

        if (purchase.Status == StorePurchaseStatus.Confirmed)
        {
            var paid = (await _trade.GetPaidByPurchaseAsync(new[] { purchase.Id }, ct)).GetValueOrDefault(purchase.Id);
            if (paid > 0)
                throw new BadRequestException("Bu alışa ödəniş yazılıb; ləğv etmək olmur.");
            if ((await _trade.GetReturnsForPurchaseAsync(purchase.Id, ct)).Count > 0)
                throw new BadRequestException("Bu alışdan təchizatçıya mal qaytarılıb; ləğv etmək olmur.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                // The goods must still be there: part of them may already have been sold.
                foreach (var group in purchase.Lines.GroupBy(l => l.StoreProductId))
                {
                    var needed = group.Sum(l => l.Quantity);
                    var balance = await _store.GetOrCreateBalanceAsync(companyId, purchase.WarehouseId, group.Key, ct);
                    if (balance.Quantity < needed)
                        throw new BadRequestException(
                            $"\"{group.First().StoreProduct.Name}\" anbarda kifayət qədər yoxdur (lazımdır: {needed}, var: {balance.Quantity}); alışı ləğv etmək olmur.");

                    var before = balance.Quantity;
                    balance.Quantity -= needed;
                    await _store.AddMovementAsync(new StoreStockMovement
                    {
                        CompanyId = companyId,
                        WarehouseId = purchase.WarehouseId,
                        StoreProductId = group.Key,
                        Type = StoreStockMovementType.PurchaseReturn,
                        Quantity = -needed,
                        BalanceBefore = before,
                        BalanceAfter = balance.Quantity,
                        UnitCost = group.First().NetUnitCost,
                        DocumentType = "Purchase",
                        DocumentId = purchase.Id,
                        DocumentNo = purchase.DocumentNo,
                        Reason = $"Alış ləğvi: {reason}",
                        CreatedByUserId = userId,
                    }, ct);
                }

                var lineSerials = purchase.Lines.Where(l => l.StoreProduct.TracksSerial)
                    .SelectMany(l => (l.SerialNumbers ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .ToList();
                if (lineSerials.Count > 0)
                {
                    foreach (var unit in await _store.FindSerialUnitsAsync(companyId, lineSerials, ct))
                    {
                        if (unit.Status != StoreSerialStatus.InStock)
                            throw new BadRequestException($"\"{unit.SerialNumber}\" artıq satılıb; alışı ləğv etmək olmur.");
                        unit.Status = StoreSerialStatus.WrittenOff;
                        unit.WarehouseId = null;
                        await _store.AddSerialEventAsync(new StoreSerialEvent
                        {
                            StoreSerialUnitId = unit.Id,
                            Type = StoreSerialEventType.WrittenOff,
                            WarehouseId = purchase.WarehouseId,
                            DocumentType = "Purchase",
                            DocumentId = purchase.Id,
                            DocumentNo = purchase.DocumentNo,
                            Note = $"Alış ləğvi: {reason}",
                            CreatedByUserId = userId,
                        }, ct);
                    }
                }

                await _trade.AddLedgerEntryAsync(new StoreLedgerEntry
                {
                    CompanyId = companyId,
                    PartyType = StoreLedgerPartyType.Supplier,
                    PartyId = purchase.StoreSupplierId,
                    Type = StoreLedgerEntryType.PurchaseCancel,
                    Amount = -purchase.Total,
                    DocumentType = "Purchase",
                    DocumentId = purchase.Id,
                    DocumentNo = purchase.DocumentNo,
                    Note = $"Ləğv: {reason}",
                    CreatedByUserId = userId,
                }, ct);

                purchase.Status = StorePurchaseStatus.Cancelled;
                purchase.CancelledAtUtc = DateTime.UtcNow;
                purchase.CancelledByUserId = userId;
                purchase.CancelReason = reason;
                await _trade.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
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
        }
        else
        {
            // A draft changed nothing, so cancelling it changes nothing either.
            purchase.Status = StorePurchaseStatus.Cancelled;
            purchase.CancelledAtUtc = DateTime.UtcNow;
            purchase.CancelledByUserId = userId;
            purchase.CancelReason = reason;
            await _trade.SaveChangesAsync(ct);
        }

        return await MapAsync(purchase, ct);
    }

    // ---- Return to supplier -----------------------------------------------------------------

    public async Task<StorePurchaseResponse> Handle(ReturnStorePurchaseCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;
        var dto = command.Request;

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new BadRequestException("Qaytarma səbəbi yazılmalıdır.");
        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new BadRequestException("Qaytarılacaq məhsul seçilməyib.");

        var purchase = await _trade.GetPurchaseAsync(command.Id, companyId, ct) ?? throw new NotFoundException("Alış tapılmadı.");
        await _access.EnsureWarehouseAsync(purchase.WarehouseId, ct);
        if (purchase.Status != StorePurchaseStatus.Confirmed)
            throw new BadRequestException("Qaytarma yalnız təsdiqlənmiş alışdan edilə bilər.");

        var previous = await _trade.GetReturnsForPurchaseAsync(purchase.Id, ct);
        var returnedQty = previous.SelectMany(r => r.Lines).GroupBy(l => l.StorePurchaseLineId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var allSerials = dto.Lines.SelectMany(l => l.SerialNumbers ?? new()).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        var serialUnits = allSerials.Count == 0 ? new List<StoreSerialUnit>() : await _store.FindSerialUnitsAsync(companyId, allSerials, ct);

        var lines = new List<(StorePurchaseReturnLine Line, StorePurchaseLine Source, List<StoreSerialUnit> Units)>();
        foreach (var req in dto.Lines)
        {
            var source = purchase.Lines.FirstOrDefault(l => l.Id == req.PurchaseLineId)
                ?? throw new BadRequestException("Seçilmiş sətir bu alışa aid deyil.");
            if (req.Quantity <= 0)
                throw new BadRequestException($"\"{source.StoreProduct.Name}\" üçün miqdar 0-dan böyük olmalıdır.");

            var remaining = source.Quantity - returnedQty.GetValueOrDefault(source.Id);
            if (req.Quantity > remaining)
                throw new BadRequestException($"\"{source.StoreProduct.Name}\" üçün ən çox {remaining} qaytarmaq olar (alınıb: {source.Quantity}).");

            var unitAmount = Math.Round(source.LineTotal / source.Quantity, 4, MidpointRounding.AwayFromZero);
            var amount = req.Quantity == remaining
                ? source.LineTotal - previous.SelectMany(r => r.Lines).Where(l => l.StorePurchaseLineId == source.Id).Sum(l => l.Amount)
                : R2(req.Quantity * unitAmount);

            var units = new List<StoreSerialUnit>();
            var serials = (req.SerialNumbers ?? new()).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            if (source.StoreProduct.TracksSerial)
            {
                if (req.Quantity != Math.Floor(req.Quantity) || serials.Count != (int)req.Quantity)
                    throw new BadRequestException($"\"{source.StoreProduct.Name}\" üçün {(int)req.Quantity} ədəd IMEI/serial seçilməlidir.");
                foreach (var serial in serials)
                {
                    var unit = serialUnits.FirstOrDefault(u => u.SerialNumber == serial);
                    if (unit is null || unit.StoreProductId != source.StoreProductId
                        || unit.Status != StoreSerialStatus.InStock || unit.WarehouseId != purchase.WarehouseId)
                        throw new BadRequestException($"\"{serial}\" bu anbarda stokda deyil.");
                    units.Add(unit);
                }
            }
            else if (serials.Count > 0)
            {
                throw new BadRequestException($"\"{source.StoreProduct.Name}\" IMEI/serial ilə izlənmir.");
            }

            lines.Add((new StorePurchaseReturnLine
            {
                StorePurchaseLineId = source.Id,
                StoreProductId = source.StoreProductId,
                ProductName = source.StoreProduct.Name,
                Quantity = req.Quantity,
                UnitAmount = unitAmount,
                Amount = amount,
            }, source, units));
        }

        var ret = new StorePurchaseReturn
        {
            CompanyId = companyId,
            DocumentNo = Guid.NewGuid().ToString("N"),
            StorePurchaseId = purchase.Id,
            StoreSupplierId = purchase.StoreSupplierId,
            WarehouseId = purchase.WarehouseId,
            Reason = dto.Reason.Trim(),
            Total = lines.Sum(l => l.Line.Amount),
            CreatedByUserId = userId,
            Lines = lines.Select(l => l.Line).ToList(),
        };

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // The goods must be on the shelf to go back.
            foreach (var group in lines.GroupBy(l => l.Source.StoreProductId))
            {
                var needed = group.Sum(l => l.Line.Quantity);
                var balance = await _store.GetOrCreateBalanceAsync(companyId, purchase.WarehouseId, group.Key, ct);
                if (balance.Quantity < needed)
                    throw new BadRequestException(
                        $"\"{group.First().Source.StoreProduct.Name}\" anbarda kifayət qədər yoxdur (lazımdır: {needed}, var: {balance.Quantity}).");
                balance.Quantity -= needed;
            }

            await _trade.AddPurchaseReturnAsync(ret, ct);
            await _trade.SaveChangesAsync(ct);
            ret.DocumentNo = $"PR-{ret.Id:D7}";

            foreach (var group in lines.GroupBy(l => l.Source.StoreProductId))
            {
                var needed = group.Sum(l => l.Line.Quantity);
                var balance = await _store.GetOrCreateBalanceAsync(companyId, purchase.WarehouseId, group.Key, ct);
                await _store.AddMovementAsync(new StoreStockMovement
                {
                    CompanyId = companyId,
                    WarehouseId = purchase.WarehouseId,
                    StoreProductId = group.Key,
                    Type = StoreStockMovementType.PurchaseReturn,
                    Quantity = -needed,
                    BalanceBefore = balance.Quantity + needed,
                    BalanceAfter = balance.Quantity,
                    UnitCost = group.First().Source.NetUnitCost,
                    DocumentType = "PurchaseReturn",
                    DocumentId = ret.Id,
                    DocumentNo = ret.DocumentNo,
                    Reason = ret.Reason,
                    CreatedByUserId = userId,
                }, ct);
            }

            foreach (var (_, _, units) in lines)
                foreach (var unit in units)
                {
                    unit.Status = StoreSerialStatus.WrittenOff;
                    unit.WarehouseId = null;
                    await _store.AddSerialEventAsync(new StoreSerialEvent
                    {
                        StoreSerialUnitId = unit.Id,
                        Type = StoreSerialEventType.WrittenOff,
                        WarehouseId = purchase.WarehouseId,
                        DocumentType = "PurchaseReturn",
                        DocumentId = ret.Id,
                        DocumentNo = ret.DocumentNo,
                        Note = "Təchizatçıya qaytarıldı",
                        CreatedByUserId = userId,
                    }, ct);
                }

            await _trade.AddLedgerEntryAsync(new StoreLedgerEntry
            {
                CompanyId = companyId,
                PartyType = StoreLedgerPartyType.Supplier,
                PartyId = purchase.StoreSupplierId,
                Type = StoreLedgerEntryType.PurchaseReturn,
                Amount = -ret.Total,
                DocumentType = "PurchaseReturn",
                DocumentId = ret.Id,
                DocumentNo = ret.DocumentNo,
                Note = ret.Reason,
                CreatedByUserId = userId,
            }, ct);

            await _trade.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
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

        return await MapAsync(purchase, ct);
    }

    // ---- Mapping ----------------------------------------------------------------------------

    private async Task<StorePurchaseResponse> MapAsync(StorePurchase p, CancellationToken ct)
    {
        var paid = (await _trade.GetPaidByPurchaseAsync(new[] { p.Id }, ct)).GetValueOrDefault(p.Id);
        var returns = await _trade.GetReturnsForPurchaseAsync(p.Id, ct);
        var returned = returns.Sum(r => r.Total);
        var returnedByLine = returns.SelectMany(r => r.Lines).GroupBy(l => l.StorePurchaseLineId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
        var names = await _store.GetUserNamesAsync(new[] { p.CreatedByUserId, p.ConfirmedByUserId }.OfType<int>(), ct);

        string? Name(int? id) => id is { } v && names.TryGetValue(v, out var n) ? n : null;

        return new StorePurchaseResponse
        {
            Id = p.Id,
            DocumentNo = p.DocumentNo,
            SupplierId = p.StoreSupplierId,
            SupplierName = p.StoreSupplier?.Name ?? string.Empty,
            WarehouseId = p.WarehouseId,
            WarehouseName = p.Warehouse?.Name ?? string.Empty,
            SupplierInvoiceNo = p.SupplierInvoiceNo,
            PurchaseDate = p.PurchaseDate,
            DueDate = p.DueDate,
            Status = p.Status,
            Note = p.Note,
            Total = p.Total,
            VatTotal = p.VatTotal,
            Paid = paid,
            Returned = returned,
            Outstanding = p.Status == StorePurchaseStatus.Confirmed ? Math.Max(0, p.Total - returned - paid) : 0m,
            CreatedAtUtc = p.CreatedAtUtc,
            CreatedByName = Name(p.CreatedByUserId),
            ConfirmedAtUtc = p.ConfirmedAtUtc,
            ConfirmedByName = Name(p.ConfirmedByUserId),
            CancelledAtUtc = p.CancelledAtUtc,
            CancelReason = p.CancelReason,
            Lines = p.Lines.OrderBy(l => l.Id).Select(l => new StorePurchaseLineResponse
            {
                Id = l.Id,
                ProductId = l.StoreProductId,
                ProductName = l.StoreProduct?.Name ?? string.Empty,
                TracksSerial = l.StoreProduct?.TracksSerial ?? false,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                VatPercent = l.VatPercent,
                LineTotal = l.LineTotal,
                NetUnitCost = l.NetUnitCost,
                ReturnedQuantity = returnedByLine.GetValueOrDefault(l.Id),
                ReturnableQuantity = p.Status == StorePurchaseStatus.Confirmed ? l.Quantity - returnedByLine.GetValueOrDefault(l.Id) : 0m,
                SerialNumbers = (l.SerialNumbers ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            }).ToList(),
            Returns = returns.Select(r => new StorePurchaseReturnResponse
            {
                Id = r.Id,
                DocumentNo = r.DocumentNo,
                CreatedAtUtc = r.CreatedAtUtc,
                Reason = r.Reason,
                Total = r.Total,
                Lines = r.Lines.Select(l => new StorePurchaseReturnLineResponse
                {
                    ProductName = l.ProductName,
                    Quantity = l.Quantity,
                    Amount = l.Amount,
                }).ToList(),
            }).ToList(),
        };
    }
}
