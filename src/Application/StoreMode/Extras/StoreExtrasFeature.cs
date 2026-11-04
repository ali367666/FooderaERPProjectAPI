using System.Text.Json;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Extras;

// ---- Quick slots (50 fast-sale buttons per till) -------------------------------------------

public record GetStoreQuickSlotsQuery(int RegisterId) : IRequest<List<StoreQuickSlotResponse>>;

public record SaveStoreQuickSlotCommand(int RegisterId, SaveStoreQuickSlotRequest Request) : IRequest<List<StoreQuickSlotResponse>>;

// ---- Held sales ----------------------------------------------------------------------------

public record GetStoreHeldSalesQuery(int RegisterId) : IRequest<List<StoreHeldSaleResponse>>;

public record HoldStoreSaleCommand(HoldStoreSaleRequest Request) : IRequest<StoreHeldSaleResponse>;

/// <summary>Takes a held basket back: returns it and removes it from the held list.</summary>
public record ResumeStoreHeldSaleCommand(int Id) : IRequest<StoreHeldSaleResponse>;

public record DiscardStoreHeldSaleCommand(int Id) : IRequest<bool>;

// ---- Serial / IMEI -------------------------------------------------------------------------

public record SearchStoreSerialsQuery(string? Term, int? ProductId, int? WarehouseId, StoreSerialStatus? Status)
    : IRequest<List<StoreSerialUnitResponse>>;

public record GetStoreSerialHistoryQuery(int Id) : IRequest<StoreSerialUnitResponse>;

public class StoreExtrasHandlers :
    IRequestHandler<GetStoreQuickSlotsQuery, List<StoreQuickSlotResponse>>,
    IRequestHandler<SaveStoreQuickSlotCommand, List<StoreQuickSlotResponse>>,
    IRequestHandler<GetStoreHeldSalesQuery, List<StoreHeldSaleResponse>>,
    IRequestHandler<HoldStoreSaleCommand, StoreHeldSaleResponse>,
    IRequestHandler<ResumeStoreHeldSaleCommand, StoreHeldSaleResponse>,
    IRequestHandler<DiscardStoreHeldSaleCommand, bool>,
    IRequestHandler<SearchStoreSerialsQuery, List<StoreSerialUnitResponse>>,
    IRequestHandler<GetStoreSerialHistoryQuery, StoreSerialUnitResponse>
{
    /// <summary>One shared set of quick buttons for the whole company; how many is a company setting.</summary>
    private async Task<int> SlotCountAsync(int companyId, CancellationToken ct)
        => (await _repository.GetOrCreateSettingsAsync(companyId, ct)).QuickSlotCount;

    private readonly IStoreRepository _repository;
    private readonly ICurrentUserService _currentUser;

    private readonly Application.StoreMode.Pricing.StorePricingService _pricing;

    public StoreExtrasHandlers(IStoreRepository repository, ICurrentUserService currentUser, Application.StoreMode.Pricing.StorePricingService pricing)
    {
        _pricing = pricing;
        _repository = repository;
        _currentUser = currentUser;
    }

    // ---- Quick slots ------------------------------------------------------------------------

    public Task<List<StoreQuickSlotResponse>> Handle(GetStoreQuickSlotsQuery query, CancellationToken ct)
        => LoadSlotsAsync(query.RegisterId, ct);

    public async Task<List<StoreQuickSlotResponse>> Handle(SaveStoreQuickSlotCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;
        var slotCount = await SlotCountAsync(companyId, ct);
        if (dto.SlotNumber < 1 || dto.SlotNumber > slotCount)
            throw new BadRequestException($"Düymə nömrəsi 1 ilə {slotCount} arasında olmalıdır.");

        _ = await _repository.GetRegisterAsync(command.RegisterId, companyId, ct)
            ?? throw new NotFoundException("Kassa tapılmadı.");

        if (dto.ProductId is { } productId
            && (await _repository.GetProductAsync(productId, companyId, ct)) is null)
            throw new BadRequestException("Məhsul tapılmadı.");

        // The buttons are shared by every till of the company: they are kept under the company's first till.
        var ownerId = await OwnerRegisterIdAsync(companyId, ct);
        var slots = await _repository.GetQuickSlotsAsync(ownerId, ct);
        var slot = slots.FirstOrDefault(s => s.SlotNumber == dto.SlotNumber);

        if (dto.ProductId is null)
        {
            if (slot is not null)
                _repository.RemoveQuickSlot(slot);
        }
        else
        {
            if (slot is null)
            {
                slot = new StoreQuickSlot
                {
                    CompanyId = companyId,
                    StoreCashRegisterId = ownerId,
                    SlotNumber = dto.SlotNumber,
                    CreatedByUserId = _currentUser.UserId,
                };
                await _repository.AddQuickSlotAsync(slot, ct);
            }

            slot.StoreProductId = dto.ProductId;
            slot.Label = string.IsNullOrWhiteSpace(dto.Label) ? null : dto.Label.Trim();
            slot.Color = string.IsNullOrWhiteSpace(dto.Color) ? null : dto.Color.Trim();
        }

        await _repository.SaveChangesAsync(ct);
        return await LoadSlotsAsync(command.RegisterId, ct);
    }

    private async Task<int> OwnerRegisterIdAsync(int companyId, CancellationToken ct)
        => (await _repository.GetRegistersAsync(companyId, null, ct)).OrderBy(r => r.Id).Select(r => (int?)r.Id).FirstOrDefault()
           ?? throw new NotFoundException("Əvvəlcə kassa yaradın.");

    private async Task<List<StoreQuickSlotResponse>> LoadSlotsAsync(int registerId, CancellationToken ct)
    {
        var register = await _repository.GetRegisterAsync(registerId, _currentUser.CompanyId, ct)
            ?? throw new NotFoundException("Kassa tapılmadı.");

        var saved = (await _repository.GetQuickSlotsAsync(await OwnerRegisterIdAsync(_currentUser.CompanyId, ct), ct)).ToDictionary(s => s.SlotNumber);
        var branchPrices = await _pricing.BranchPricesAsync(register.RestaurantId, saved.Values.Select(s => s.StoreProductId).OfType<int>().ToList(), ct);
        var count = await SlotCountAsync(_currentUser.CompanyId, ct);
        return Enumerable.Range(1, count).Select(n =>
        {
            saved.TryGetValue(n, out var s);
            return new StoreQuickSlotResponse
            {
                SlotNumber = n,
                ProductId = s?.StoreProductId,
                ProductName = s?.StoreProduct?.Name,
                SalePrice = s?.StoreProduct is { } sp
                    ? Application.StoreMode.Pricing.StorePricingService.Effective(sp.SalePrice, sp.CampaignPrice, sp.CampaignFromUtc, sp.CampaignToUtc, branchPrices.TryGetValue(sp.Id, out var bp) && bp > 0 ? bp : null, DateTime.UtcNow)
                    : null,
                Unit = s?.StoreProduct?.Unit,
                Label = s?.Label,
                Color = s?.Color,
            };
        }).ToList();
    }

    // ---- Held sales -------------------------------------------------------------------------

    public async Task<List<StoreHeldSaleResponse>> Handle(GetStoreHeldSalesQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        _ = await _repository.GetRegisterAsync(query.RegisterId, companyId, ct)
            ?? throw new NotFoundException("Kassa tapılmadı.");

        var rows = await _repository.GetHeldSalesAsync(companyId, query.RegisterId, ct);
        var names = await _repository.GetUserNamesAsync(rows.Select(r => r.CashierUserId), ct);
        return rows.Select(r => MapHeld(r, names)).ToList();
    }

    public async Task<StoreHeldSaleResponse> Handle(HoldStoreSaleCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var userId = _currentUser.UserId;
        var dto = command.Request;

        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new BadRequestException("Səbət boşdur.");

        var register = await _repository.GetRegisterAsync(dto.StoreCashRegisterId, companyId, ct)
            ?? throw new BadRequestException("Kassa tapılmadı.");
        var shift = await _repository.GetOpenShiftForRegisterAsync(register.Id, ct)
            ?? throw new BadRequestException("Satışı saxlamaq üçün növbə açıq olmalıdır.");
        if (shift.CashierUserId != userId)
            throw new BadRequestException("Bu kassada başqa kassirin növbəsi açıqdır.");

        var products = (await _repository.GetProductsByIdsAsync(companyId, dto.Lines.Select(l => l.ProductId).Distinct().ToList(), ct))
            .ToDictionary(p => p.Id);
        decimal total = 0;
        foreach (var line in dto.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var p))
                throw new BadRequestException("Səbətdəki məhsullardan biri tapılmadı.");
            if (line.Quantity <= 0)
                throw new BadRequestException("Miqdar 0-dan böyük olmalıdır.");
            total += Math.Round(line.Quantity * p.EffectivePrice(DateTime.UtcNow), 2, MidpointRounding.AwayFromZero) - line.DiscountAmount;
        }

        var held = new StoreHeldSale
        {
            CompanyId = companyId,
            StoreCashRegisterId = register.Id,
            CashierUserId = userId,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title.Trim(),
            CartJson = JsonSerializer.Serialize(dto.Lines),
            ItemCount = dto.Lines.Count,
            Total = Math.Round(total, 2, MidpointRounding.AwayFromZero),
            CreatedByUserId = userId,
        };
        await _repository.AddHeldSaleAsync(held, ct);
        await _repository.SaveChangesAsync(ct);

        var names = await _repository.GetUserNamesAsync(new[] { userId }, ct);
        return MapHeld(held, names);
    }

    public async Task<StoreHeldSaleResponse> Handle(ResumeStoreHeldSaleCommand command, CancellationToken ct)
    {
        var held = await _repository.GetHeldSaleAsync(command.Id, _currentUser.CompanyId, ct)
            ?? throw new NotFoundException("Saxlanmış satış tapılmadı (artıq götürülüb ola bilər).");
        var names = await _repository.GetUserNamesAsync(new[] { held.CashierUserId }, ct);
        var response = MapHeld(held, names);

        _repository.RemoveHeldSale(held);
        await _repository.SaveChangesAsync(ct);
        return response;
    }

    public async Task<bool> Handle(DiscardStoreHeldSaleCommand command, CancellationToken ct)
    {
        var held = await _repository.GetHeldSaleAsync(command.Id, _currentUser.CompanyId, ct)
            ?? throw new NotFoundException("Saxlanmış satış tapılmadı.");
        _repository.RemoveHeldSale(held);
        await _repository.SaveChangesAsync(ct);
        return true;
    }

    private static StoreHeldSaleResponse MapHeld(StoreHeldSale h, IReadOnlyDictionary<int, string> names) => new()
    {
        Id = h.Id,
        Title = h.Title,
        CreatedAtUtc = h.CreatedAtUtc,
        ItemCount = h.ItemCount,
        Total = h.Total,
        CashierUserId = h.CashierUserId,
        CashierName = names.TryGetValue(h.CashierUserId, out var n) ? n : null,
        Lines = JsonSerializer.Deserialize<List<HeldCartLine>>(h.CartJson) ?? new(),
    };

    // ---- Serial / IMEI ----------------------------------------------------------------------

    public async Task<List<StoreSerialUnitResponse>> Handle(SearchStoreSerialsQuery query, CancellationToken ct)
    {
        var rows = await _repository.SearchSerialUnitsAsync(
            _currentUser.CompanyId, query.Term, query.ProductId, query.WarehouseId, query.Status, 200, ct);
        return rows.Select(u => MapSerial(u, false)).ToList();
    }

    public async Task<StoreSerialUnitResponse> Handle(GetStoreSerialHistoryQuery query, CancellationToken ct)
    {
        var unit = await _repository.GetSerialUnitWithHistoryAsync(query.Id, _currentUser.CompanyId, ct)
            ?? throw new NotFoundException("Serial nömrə tapılmadı.");
        return MapSerial(unit, true);
    }

    private static StoreSerialUnitResponse MapSerial(StoreSerialUnit u, bool withHistory) => new()
    {
        Id = u.Id,
        SerialNumber = u.SerialNumber,
        ProductId = u.StoreProductId,
        ProductName = u.StoreProduct?.Name ?? string.Empty,
        WarehouseId = u.WarehouseId,
        WarehouseName = u.Warehouse?.Name,
        Status = u.Status,
        SoldAtUtc = u.SoldAtUtc,
        History = withHistory
            ? u.Events.OrderBy(e => e.CreatedAtUtc).ThenBy(e => e.Id).Select(e => new StoreSerialEventResponse
            {
                Type = e.Type,
                CreatedAtUtc = e.CreatedAtUtc,
                DocumentType = e.DocumentType,
                DocumentNo = e.DocumentNo,
                Note = e.Note,
            }).ToList()
            : new(),
    };
}
