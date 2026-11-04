using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.StoreMode.Approvals;
using Domain.Constants;
using Domain.Entities.StoreMode;
using Domain.Enums;

namespace Application.StoreMode.Sales;

/// <summary>A sale that passed every check and is ready to be saved (by <see cref="StoreSaleProcessor.PersistAsync"/>).</summary>
public sealed class PreparedSale
{
    public StoreSale Sale { get; init; } = default!;
    public int WarehouseId { get; init; }
    public Dictionary<int, StoreStockBalance> Balances { get; init; } = new();
    public List<(StoreSaleLine Line, List<StoreSerialUnit> Units)> SerialLines { get; init; } = new();
    public List<(string Action, int ApproverId, string? Reason)> Approvals { get; init; } = new();

    /// <summary>Each product's minimum stock, so the sale can tell when it pushed one below it.</summary>
    public Dictionary<int, decimal> MinimumStock { get; init; } = new();

    /// <summary>Products this sale took to or below their minimum (name, quantity left, minimum).</summary>
    public List<(string Name, decimal Quantity, decimal Minimum)> LowStock { get; } = new();
}

/// <summary>
/// Validates and saves a till sale. Shared by a plain sale and the sale half of an exchange: it does no
/// transaction handling of its own, the caller wraps it in one.
/// </summary>
public sealed class StoreSaleProcessor
{
    private readonly IStoreRepository _repository;
    private readonly IStoreCustomerRepository _customers;
    private readonly Application.StoreMode.Customers.StoreBonusExpiryService _bonusExpiry;
    private readonly Application.StoreMode.Fiscal.StoreFiscalService _fiscal;
    private readonly Application.StoreMode.Access.StoreAccessService _access;
    private readonly Application.StoreMode.Pricing.StorePricingService _pricing;
    private readonly IStoreTradeRepository _trade;
    private readonly ICurrentUserService _currentUser;
    private readonly StoreApprovalService _approvals;

    public StoreSaleProcessor(
        IStoreRepository repository,
        IStoreCustomerRepository customers,
        IStoreTradeRepository trade,
        ICurrentUserService currentUser,
        StoreApprovalService approvals,
        Application.StoreMode.Customers.StoreBonusExpiryService bonusExpiry,
        Application.StoreMode.Fiscal.StoreFiscalService fiscal,
        Application.StoreMode.Access.StoreAccessService access,
        Application.StoreMode.Pricing.StorePricingService pricing)
    {
        _pricing = pricing;
        _access = access;
        _fiscal = fiscal;
        _repository = repository;
        _customers = customers;
        _trade = trade;
        _currentUser = currentUser;
        _approvals = approvals;
        _bonusExpiry = bonusExpiry;
    }

    private static decimal StorePricingServiceStatic(StoreProduct product, IReadOnlyDictionary<int, decimal> branchPrices)
        => Application.StoreMode.Pricing.StorePricingService.Effective(
            product.SalePrice, product.CampaignPrice, product.CampaignFromUtc, product.CampaignToUtc,
            branchPrices.TryGetValue(product.Id, out var bp) && bp > 0 ? bp : null, DateTime.UtcNow);

    /// <summary>The part of the sale made of products that earn bonus (1 when all do).</summary>
    /// <summary>The categories that give no bonus: those switched off and every category under them.</summary>
    private async Task<HashSet<int>> NoBonusCategoryIdsAsync(int companyId, CancellationToken ct)
    {
        var all = await _repository.GetCategoriesAsync(companyId, ct);
        var off = all.Where(c => !c.EarnsBonus).Select(c => c.Id).ToHashSet();
        bool changed;
        do
        {
            changed = false;
            foreach (var c in all)
                if (c.ParentCategoryId is { } parent && off.Contains(parent) && off.Add(c.Id))
                    changed = true;
        }
        while (changed);
        return off;
    }

    private static decimal BonusEligibleShare(IEnumerable<StoreSaleLine> lines, IReadOnlyDictionary<int, StoreProduct> products, ISet<int> noBonusCategories)
    {
        var all = lines.ToList();
        var total = all.Sum(l => l.LineTotal);
        if (total <= 0) return 0m;
        return all.Where(l => products.TryGetValue(l.StoreProductId, out var p) && p.EarnsBonus && !(p.CategoryId is { } cid && noBonusCategories.Contains(cid))).Sum(l => l.LineTotal) / total;
    }

    public static decimal R2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <param name="returnCredit">Value of goods handed back in an exchange, counted as payment (0 for a plain sale).</param>
    /// <param name="incomingStock">Per product, what an exchange puts back on the shelf before this sale (counts as available).</param>
    public async Task<PreparedSale> PrepareAsync(
        CreateStoreSaleRequest dto, decimal returnCredit, Dictionary<int, decimal>? incomingStock, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;

        if (dto.OperationId == Guid.Empty)
            throw new BadRequestException("Əməliyyat identifikatoru (operationId) göndərilməlidir.");
        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new BadRequestException("Səbət boşdur.");
        dto.Payments ??= new();

        var register = await _repository.GetRegisterAsync(dto.StoreCashRegisterId, companyId, ct)
            ?? throw new BadRequestException("Kassa tapılmadı.");
        if (!register.IsActive)
            throw new BadRequestException("Bu kassa aktiv deyil.");
        await _access.EnsureBranchAsync(register.RestaurantId, ct);

        var shift = await _repository.GetOpenShiftForRegisterAsync(register.Id, ct)
            ?? throw new BadRequestException("Satış üçün əvvəlcə növbəni açın.");
        if (shift.CashierUserId != userId)
            throw new BadRequestException("Bu kassada başqa kassirin növbəsi açıqdır.");

        var settings = await _repository.GetOrCreateSettingsAsync(companyId, ct);
        var approvals = new List<(string, int, string?)>();

        // Prices, names and VAT come from the server; whatever the client displayed is not trusted.
        var productIds = dto.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = (await _repository.GetProductsByIdsAsync(companyId, productIds, ct)).ToDictionary(p => p.Id);
        var branchPrices = await _pricing.BranchPricesAsync(register.RestaurantId, productIds, ct);

        var allSerials = dto.Lines.SelectMany(l => l.SerialNumbers ?? new()).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        if (allSerials.Count != allSerials.Distinct().Count())
            throw new BadRequestException("Eyni IMEI/serial nömrəsi iki dəfə seçilib.");
        var serialUnits = allSerials.Count == 0
            ? new List<StoreSerialUnit>()
            : await _repository.FindSerialUnitsAsync(companyId, allSerials, ct);

        var lines = new List<StoreSaleLine>();
        var serialLines = new List<(StoreSaleLine, List<StoreSerialUnit>)>();
        var priceChanged = false;

        foreach (var line in dto.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product) || !product.IsActive)
                throw new BadRequestException("Səbətdəki məhsullardan biri tapılmadı və ya aktiv deyil.");
            if (line.Quantity <= 0)
                throw new BadRequestException($"\"{product.Name}\" üçün miqdar 0-dan böyük olmalıdır.");
            if (product.Unit == UnitOfMeasure.Piece && line.Quantity != Math.Floor(line.Quantity))
                throw new BadRequestException($"\"{product.Name}\" ədədlə satılır, miqdar tam olmalıdır.");

            var current = StorePricingServiceStatic(product, branchPrices);
            var price = current;
            string? priceReason = null;
            if (line.OverridePrice is { } overridePrice && overridePrice != current)
            {
                if (overridePrice < 0)
                    throw new BadRequestException($"\"{product.Name}\" üçün qiymət mənfi ola bilməz.");
                if (string.IsNullOrWhiteSpace(line.PriceChangeReason))
                    throw new BadRequestException($"\"{product.Name}\" üçün qiymət dəyişmə səbəbi yazılmalıdır.");
                price = R2(overridePrice);
                priceReason = line.PriceChangeReason.Trim();
                priceChanged = true;
            }

            var gross = R2(line.Quantity * price);
            var discount = R2(line.DiscountAmount);
            if (discount < 0 || discount > gross)
                throw new BadRequestException($"\"{product.Name}\" üçün endirim 0 ilə {gross:0.00} arasında olmalıdır.");

            var lineTotal = gross - discount;
            var vatPercent = product.VatPercent ?? 0m;

            var saleLine = new StoreSaleLine
            {
                StoreProductId = product.Id,
                ProductName = product.Name,
                Barcode = product.Barcodes.OrderByDescending(b => b.IsPrimary).Select(b => b.Barcode).FirstOrDefault(),
                Sku = product.Sku,
                Quantity = line.Quantity,
                UnitPrice = price,
                ListPrice = current,
                PriceChangeReason = priceReason,
                DiscountAmount = discount,
                VatPercent = vatPercent,
                // Prices include VAT, so the VAT inside a total is total * v / (100 + v).
                VatAmount = vatPercent > 0 ? R2(lineTotal * vatPercent / (100 + vatPercent)) : 0m,
                LineTotal = lineTotal,
                UnitCost = product.AverageCost > 0 ? product.AverageCost : product.PurchasePrice,
            };
            lines.Add(saleLine);

            var wanted = (line.SerialNumbers ?? new()).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            if (product.TracksSerial)
            {
                if (wanted.Count != (int)line.Quantity)
                    throw new BadRequestException($"\"{product.Name}\" üçün {(int)line.Quantity} ədəd IMEI/serial seçilməlidir (seçilib: {wanted.Count}).");

                var units = new List<StoreSerialUnit>();
                foreach (var serial in wanted)
                {
                    var unit = serialUnits.FirstOrDefault(u => u.SerialNumber == serial);
                    if (unit is null || unit.StoreProductId != product.Id)
                        throw new BadRequestException($"\"{serial}\" nömrəsi bu məhsul üçün tapılmadı.");
                    if (unit.Status != StoreSerialStatus.InStock || unit.WarehouseId != register.WarehouseId)
                        throw new BadRequestException($"\"{serial}\" bu kassanın anbarında satışa hazır deyil.");
                    units.Add(unit);
                }

                serialLines.Add((saleLine, units));
            }
            else if (wanted.Count > 0)
            {
                throw new BadRequestException($"\"{product.Name}\" IMEI/serial ilə izlənmir.");
            }
        }

        if (dto.Receiptless && (!settings.AllowReceiptlessSale || !_currentUser.HasPermission(AppPermissions.StoreSaleNoReceipt)))
            throw new BadRequestException("Çeksiz satışa icazə verilməyib.");

        var subTotal = lines.Sum(l => R2(l.Quantity * l.UnitPrice));
        var discountTotal = lines.Sum(l => l.DiscountAmount);
        var total = lines.Sum(l => l.LineTotal);
        if (total <= 0)
            throw new BadRequestException("Satışın məbləği 0-dan böyük olmalıdır.");

        // Manager approval where the cashier's own permissions fall short.
        if (priceChanged
            && await _approvals.RequireAsync(AppPermissions.StoreSaleChangePrice, dto.Approval, "qiymət dəyişmək", ct) is { } priceApprover)
            approvals.Add(("PriceChange", priceApprover, dto.Approval?.Reason));
        if (discountTotal > 0
            && await _approvals.RequireAsync(AppPermissions.StoreSaleDiscount, dto.Approval, "endirim vermək", ct) is { } discountApprover)
            approvals.Add(("Discount", discountApprover, dto.Approval?.Reason));
        if (discountTotal > 0 && subTotal > 0 && discountTotal * 100m > settings.MaxDiscountPercent * subTotal
            && await _approvals.RequireAsync(
                AppPermissions.StoreSaleApprove, dto.Approval,
                $"endirim {settings.MaxDiscountPercent:0.##}% limitini aşır", ct) is { } limitApprover)
            approvals.Add(("DiscountOverLimit", limitApprover, dto.Approval?.Reason));

        // Payments: cash and card only from the customer; card cannot exceed what is due, cash covers the rest and gives change.
        var creditUsed = Math.Min(R2(returnCredit), total);
        var payable = total - creditUsed;

        // The customer card: needed for bonus, credit and earning bonus.
        StoreCustomer? customer = null;
        if (dto.CustomerId is { } customerId)
        {
            customer = await _customers.GetCustomerAsync(customerId, companyId, ct)
                ?? throw new BadRequestException("Müştəri tapılmadı.");
            if (!customer.IsActive)
                throw new BadRequestException("Bu müştəri kartı aktiv deyil.");
        }

        var payments = new List<StoreSalePayment>();
        foreach (var p in dto.Payments)
        {
            if (p.Method is not (StorePaymentMethod.Cash or StorePaymentMethod.Card or StorePaymentMethod.Bonus or StorePaymentMethod.Debt))
                throw new BadRequestException("Bu ödəniş üsulu dəstəklənmir.");
            if (R2(p.Amount) <= 0)
                throw new BadRequestException("Ödəniş məbləği 0-dan böyük olmalıdır.");
            if (p.Method is StorePaymentMethod.Bonus or StorePaymentMethod.Debt && customer is null)
                throw new BadRequestException("Bonus və ya borc üçün müştərini seçin.");
            payments.Add(new StoreSalePayment { Method = p.Method, Amount = R2(p.Amount), Reference = p.Reference?.Trim() });
        }

        var card = payments.Where(p => p.Method == StorePaymentMethod.Card).Sum(p => p.Amount);
        var cash = payments.Where(p => p.Method == StorePaymentMethod.Cash).Sum(p => p.Amount);
        var bonus = payments.Where(p => p.Method == StorePaymentMethod.Bonus).Sum(p => p.Amount);
        var debt = payments.Where(p => p.Method == StorePaymentMethod.Debt).Sum(p => p.Amount);

        if (bonus > 0)
        {
            await _bonusExpiry.ExpireAsync(companyId, customer!.Id, ct);
            var bonusBalance = await _customers.GetBonusBalanceAsync(companyId, customer!.Id, ct);
            if (bonus > bonusBalance)
                throw new BadRequestException($"Müştərinin bonus balansı {bonusBalance:0.00}, bundan çox xərcləmək olmur.");
            var bonusLimit = R2(payable * settings.BonusMaxPaymentPercent / 100m);
            if (bonus > bonusLimit)
                throw new BadRequestException($"Bonusla ən çox {bonusLimit:0.00} AZN ({settings.BonusMaxPaymentPercent:0.##}%) ödəmək olar.");
        }

        if (debt > 0)
        {
            // Selling on credit needs the permission (or a manager) and stays inside the customer's credit limit.
            if (await _approvals.RequireAsync(AppPermissions.StoreSaleDebt, dto.Approval, "borca satış", ct) is { } debtApprover)
                approvals.Add(("CreditSale", debtApprover, dto.Approval?.Reason));
            var currentDebt = await _trade.GetLedgerBalanceAsync(companyId, StoreLedgerPartyType.Customer, customer!.Id, ct);
            if (customer.CreditLimit <= 0)
                throw new BadRequestException("Bu müştəriyə kredit limiti verilməyib; borca satış olmaz.");
            if (currentDebt + debt > customer.CreditLimit)
                throw new BadRequestException(
                    $"Kredit limiti aşılır: limit {customer.CreditLimit:0.00}, indiki borc {Math.Max(0, currentDebt):0.00}, bu satışdan {debt:0.00}.");
        }

        var nonCash = card + bonus + debt;
        if (nonCash > payable)
            throw new BadRequestException("Kart, bonus və borc cəmi ödənilməli məbləği aşa bilməz.");
        var cashDue = payable - nonCash;
        if (cash < cashDue)
            throw new BadRequestException($"Ödəniş kifayət etmir: {payable:0.00} AZN lazımdır, {nonCash + cash:0.00} AZN daxil edilib.");
        var change = cash - cashDue;

        // Bonus is earned on what the customer really paid (not on bonus, credit or exchange credit).
        // A customer's own percent (e.g. a VIP) replaces the company's normal one.
        var earnPercent = customer?.BonusPercentOverride ?? settings.BonusEarnPercent;
        var bonusEarned = customer is not null && earnPercent > 0
            ? R2((payable - bonus - debt) * earnPercent / 100m * BonusEligibleShare(lines, products, await NoBonusCategoryIdsAsync(companyId, ct)))
            : 0m;

        if (creditUsed > 0)
            payments.Add(new StoreSalePayment { Method = StorePaymentMethod.ReturnCredit, Amount = creditUsed });

        // Stock: the same product on two lines counts together.
        var balances = new Dictionary<int, StoreStockBalance>();
        foreach (var group in lines.GroupBy(l => l.StoreProductId))
        {
            var balance = await _repository.GetOrCreateBalanceAsync(companyId, register.WarehouseId, group.Key, ct);
            var needed = group.Sum(l => l.Quantity);
            var available = balance.Quantity + (incomingStock?.GetValueOrDefault(group.Key) ?? 0m);
            if (!settings.AllowNegativeStock && available < needed)
                throw new BadRequestException(
                    $"\"{products[group.Key].Name}\" anbarda kifayət qədər yoxdur (lazımdır: {needed}, var: {available}).");
            balances[group.Key] = balance;
        }

        var sale = new StoreSale
        {
            CompanyId = companyId,
            DocumentNo = Guid.NewGuid().ToString("N"),
            OperationId = dto.OperationId,
            RestaurantId = register.RestaurantId,
            WarehouseId = register.WarehouseId,
            StoreCashRegisterId = register.Id,
            StoreCashRegister = register,
            StoreCashShiftId = shift.Id,
            Status = StoreSaleStatus.Completed,
            SubTotal = subTotal,
            DiscountTotal = discountTotal,
            VatTotal = lines.Sum(l => l.VatAmount),
            Total = total,
            PaidTotal = card + cash + bonus + creditUsed,
            StoreCustomerId = customer?.Id,
            BonusEarned = bonusEarned,
            BonusSpent = bonus,
            DebtAmount = debt,
            ChangeAmount = change,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
            IsReceiptless = dto.Receiptless,
            CreatedByUserId = userId,
            Lines = lines,
            Payments = payments,
        };

        return new PreparedSale
        {
            Sale = sale,
            WarehouseId = register.WarehouseId,
            Balances = balances,
            SerialLines = serialLines,
            Approvals = approvals,
            MinimumStock = products.ToDictionary(x => x.Key, x => x.Value.MinimumStock),
        };
    }

    /// <summary>Saves the sale, takes the goods out of stock and marks the serial units sold. Run inside the caller's transaction.</summary>
    public async Task PersistAsync(PreparedSale prepared, CancellationToken ct)
    {
        var sale = prepared.Sale;
        var userId = _currentUser.UserId;

        await _repository.AddSaleAsync(sale, ct);
        await _repository.SaveChangesAsync(ct);
        sale.DocumentNo = $"S-{sale.Id:D7}";

        foreach (var group in sale.Lines.GroupBy(l => l.StoreProductId))
        {
            var balance = prepared.Balances[group.Key];
            var quantity = group.Sum(l => l.Quantity);
            var before = balance.Quantity;
            balance.Quantity -= quantity;

            var minimum = prepared.MinimumStock.GetValueOrDefault(group.Key);
            if (minimum > 0 && before > minimum && balance.Quantity <= minimum)
                prepared.LowStock.Add((group.First().ProductName, balance.Quantity, minimum));

            await _repository.AddMovementAsync(new StoreStockMovement
            {
                CompanyId = sale.CompanyId,
                WarehouseId = prepared.WarehouseId,
                StoreProductId = group.Key,
                Type = StoreStockMovementType.Sale,
                Quantity = -quantity,
                BalanceBefore = before,
                BalanceAfter = balance.Quantity,
                UnitCost = group.First().UnitCost,
                DocumentType = "Sale",
                DocumentId = sale.Id,
                DocumentNo = sale.DocumentNo,
                CreatedByUserId = userId,
            }, ct);
        }

        var now = DateTime.UtcNow;
        foreach (var (line, units) in prepared.SerialLines)
        {
            foreach (var unit in units)
            {
                unit.Status = StoreSerialStatus.Sold;
                unit.WarehouseId = null;
                unit.StoreSaleLineId = line.Id;
                unit.SoldAtUtc = now;
                await _repository.AddSerialEventAsync(new StoreSerialEvent
                {
                    StoreSerialUnitId = unit.Id,
                    Type = StoreSerialEventType.Sold,
                    WarehouseId = prepared.WarehouseId,
                    DocumentType = "Sale",
                    DocumentId = sale.Id,
                    DocumentNo = sale.DocumentNo,
                    CreatedByUserId = userId,
                }, ct);
            }
        }

        if (sale.StoreCustomerId is { } customerId)
        {
            if (sale.DebtAmount > 0)
                await _trade.AddLedgerEntryAsync(new StoreLedgerEntry
                {
                    CompanyId = sale.CompanyId,
                    PartyType = StoreLedgerPartyType.Customer,
                    PartyId = customerId,
                    Type = StoreLedgerEntryType.CreditSale,
                    Amount = sale.DebtAmount,
                    DocumentType = "Sale",
                    DocumentId = sale.Id,
                    DocumentNo = sale.DocumentNo,
                    CreatedByUserId = userId,
                }, ct);
            if (sale.BonusSpent > 0)
                await _customers.AddBonusEntryAsync(new StoreBonusEntry
                {
                    CompanyId = sale.CompanyId,
                    StoreCustomerId = customerId,
                    Type = StoreBonusEntryType.Spent,
                    Amount = -sale.BonusSpent,
                    DocumentType = "Sale",
                    DocumentId = sale.Id,
                    DocumentNo = sale.DocumentNo,
                    CreatedByUserId = userId,
                }, ct);
            if (sale.BonusEarned > 0)
                await _customers.AddBonusEntryAsync(new StoreBonusEntry
                {
                    CompanyId = sale.CompanyId,
                    StoreCustomerId = customerId,
                    Type = StoreBonusEntryType.Earned,
                    Amount = sale.BonusEarned,
                    DocumentType = "Sale",
                    DocumentId = sale.Id,
                    DocumentNo = sale.DocumentNo,
                    CreatedByUserId = userId,
                }, ct);
        }

        foreach (var (action, approverId, reason) in prepared.Approvals)
            await _approvals.LogAsync(action, "Sale", sale.Id, sale.DocumentNo, approverId, reason, ct);

        await _repository.SaveChangesAsync(ct);

        // After the sale is safely saved: the fiscal device may be slow or down and must not undo it.
        await _fiscal.RegisterAsync(sale, force: false, ct);
    }

    public async Task<StoreSaleResponse> MapAsync(StoreSale s, CancellationToken ct)
    {
        var names = await _repository.GetUserNamesAsync(new[] { s.CreatedByUserId, s.CancelledByUserId }.OfType<int>(), ct);
        var serials = (await _repository.GetSerialUnitsForSaleLinesAsync(s.Lines.Select(l => l.Id).ToList(), ct))
            .GroupBy(u => u.StoreSaleLineId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(u => u.SerialNumber).OrderBy(x => x).ToList());

        return new StoreSaleResponse
        {
            Id = s.Id,
            DocumentNo = s.DocumentNo,
            OperationId = s.OperationId,
            Status = s.Status,
            CreatedAtUtc = s.CreatedAtUtc,
            RestaurantId = s.RestaurantId,
            BranchName = s.StoreCashRegister?.Restaurant?.Name,
            StoreCashRegisterId = s.StoreCashRegisterId,
            RegisterName = s.StoreCashRegister?.Name,
            StoreCashShiftId = s.StoreCashShiftId,
            CashierUserId = s.CreatedByUserId,
            CashierName = s.CreatedByUserId is { } id && names.TryGetValue(id, out var n) ? n : null,
            CustomerId = s.StoreCustomerId,
            CustomerName = s.StoreCustomer?.Name,
            BonusEarned = s.BonusEarned,
            BonusSpent = s.BonusSpent,
            DebtAmount = s.DebtAmount,
            SubTotal = s.SubTotal,
            DiscountTotal = s.DiscountTotal,
            VatTotal = s.VatTotal,
            Total = s.Total,
            PaidTotal = s.PaidTotal,
            ChangeAmount = s.ChangeAmount,
            Note = s.Note,
            CancelledAtUtc = s.CancelledAtUtc,
            CancelReason = s.CancelReason,
            IsReceiptless = s.IsReceiptless,
            FiscalStatus = s.FiscalStatus,
            FiscalReceiptNo = s.FiscalReceiptNo,
            FiscalError = s.FiscalError,
            Lines = s.Lines.OrderBy(l => l.Id).Select(l => new StoreSaleLineResponse
            {
                LineId = l.Id,
                ProductId = l.StoreProductId,
                ProductName = l.ProductName,
                Barcode = l.Barcode,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                ListPrice = l.ListPrice,
                PriceChangeReason = l.PriceChangeReason,
                DiscountAmount = l.DiscountAmount,
                VatPercent = l.VatPercent,
                VatAmount = l.VatAmount,
                LineTotal = l.LineTotal,
                SerialNumbers = serials.GetValueOrDefault(l.Id) ?? new(),
            }).ToList(),
            Payments = s.Payments.OrderBy(p => p.Id).Select(p => new StoreSalePaymentResponse
            {
                Method = p.Method,
                Amount = p.Amount,
                Reference = p.Reference,
            }).ToList(),
        };
    }
}
