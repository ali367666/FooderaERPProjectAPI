using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Catalog;

internal static class StoreProductMapper
{
    public static StoreProductResponse ToResponse(StoreProduct p, decimal? stock = null)
    {
        var codes = p.Barcodes.OrderByDescending(b => b.IsPrimary).ThenBy(b => b.Id).Select(b => b.Barcode).ToList();
        return new StoreProductResponse
        {
            Id = p.Id,
            Name = p.Name,
            Sku = p.Sku,
            PrimaryBarcode = codes.FirstOrDefault(),
            Barcodes = codes,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            Brand = p.Brand,
            Unit = p.Unit,
            PurchasePrice = p.PurchasePrice,
            SalePrice = p.SalePrice,
            WholesalePrice = p.WholesalePrice,
            CampaignPrice = p.CampaignPrice,
            CampaignFromUtc = p.CampaignFromUtc,
            CampaignToUtc = p.CampaignToUtc,
            EffectivePrice = p.EffectivePrice(DateTime.UtcNow),
            VatPercent = p.VatPercent,
            AverageCost = p.AverageCost,
            MinimumStock = p.MinimumStock,
            TracksSerial = p.TracksSerial,
            EarnsBonus = p.EarnsBonus,
            ImageUrl = p.ImageUrl,
            IsActive = p.IsActive,
            Stock = stock,
        };
    }
}

// ---- Categories ----------------------------------------------------------------------------

public record GetStoreCategoriesQuery : IRequest<List<StoreCategoryResponse>>;

public record SaveStoreCategoryCommand(int? Id, SaveStoreCategoryRequest Request) : IRequest<StoreCategoryResponse>;

public class StoreCategoryHandlers :
    IRequestHandler<GetStoreCategoriesQuery, List<StoreCategoryResponse>>,
    IRequestHandler<SaveStoreCategoryCommand, StoreCategoryResponse>
{
    private readonly IStoreRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public StoreCategoryHandlers(IStoreRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<List<StoreCategoryResponse>> Handle(GetStoreCategoriesQuery query, CancellationToken ct)
    {
        var rows = await _repository.GetCategoriesAsync(_currentUser.CompanyId, ct);
        return rows.Select(Map).ToList();
    }

    public async Task<StoreCategoryResponse> Handle(SaveStoreCategoryCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BadRequestException("Kateqoriya adı boş ola bilməz.");

        if (dto.ParentCategoryId is { } parentId)
        {
            if (parentId == command.Id)
                throw new BadRequestException("Kateqoriya özünün alt kateqoriyası ola bilməz.");
            if (await _repository.GetCategoryAsync(parentId, companyId, ct) is null)
                throw new BadRequestException("Ana kateqoriya tapılmadı.");
        }

        StoreCategory category;
        if (command.Id is { } id)
        {
            category = await _repository.GetCategoryAsync(id, companyId, ct)
                ?? throw new NotFoundException("Kateqoriya tapılmadı.");
        }
        else
        {
            category = new StoreCategory { CompanyId = companyId, CreatedByUserId = _currentUser.UserId };
            await _repository.AddCategoryAsync(category, ct);
        }

        category.Name = dto.Name.Trim();
        category.ParentCategoryId = dto.ParentCategoryId;
        category.IsActive = dto.IsActive;
        category.EarnsBonus = dto.EarnsBonus;
        await _repository.SaveChangesAsync(ct);
        return Map(category);
    }

    private static StoreCategoryResponse Map(StoreCategory c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        ParentCategoryId = c.ParentCategoryId,
        IsActive = c.IsActive,
        EarnsBonus = c.EarnsBonus,
    };
}

// ---- Products ------------------------------------------------------------------------------

public record SearchStoreProductsQuery(string? Term, int? CategoryId, bool? IsActive, int Page, int PageSize, int? WarehouseId)
    : IRequest<StoreProductPageResponse>;

/// <summary>The POS scan: one active product by barcode or SKU, with the stock of the till's warehouse.</summary>
public record FindStoreProductByCodeQuery(string Code, int? WarehouseId) : IRequest<StoreProductResponse>;

public record GetStoreProductQuery(int Id, int? WarehouseId = null) : IRequest<StoreProductResponse>;

public record SaveStoreProductCommand(int? Id, SaveStoreProductRequest Request) : IRequest<StoreProductResponse>;

public class StoreProductHandlers :
    IRequestHandler<SearchStoreProductsQuery, StoreProductPageResponse>,
    IRequestHandler<FindStoreProductByCodeQuery, StoreProductResponse>,
    IRequestHandler<GetStoreProductQuery, StoreProductResponse>,
    IRequestHandler<SaveStoreProductCommand, StoreProductResponse>
{
    private readonly IStoreRepository _repository;
    private readonly IStoreTradeRepository _trade;
    private readonly ICurrentUserService _currentUser;

    private readonly Application.StoreMode.Pricing.StorePricingService _pricing;

    public StoreProductHandlers(IStoreRepository repository, IStoreTradeRepository trade, ICurrentUserService currentUser, Application.StoreMode.Pricing.StorePricingService pricing)
    {
        _pricing = pricing;
        _repository = repository;
        _trade = trade;
        _currentUser = currentUser;
    }

    public async Task<StoreProductPageResponse> Handle(SearchStoreProductsQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var pageSize = Math.Clamp(query.PageSize, 1, 2000);
        var page = Math.Max(1, query.Page);

        var (items, total) = await _repository.SearchProductsAsync(
            companyId, query.Term, query.CategoryId, query.IsActive, (page - 1) * pageSize, pageSize, ct);

        Dictionary<int, decimal>? stock = null;
        if (query.WarehouseId is { } warehouseId)
            stock = await _repository.GetWarehouseQuantitiesAsync(companyId, warehouseId, items.Select(i => i.Id).ToList(), ct);

        var result = new StoreProductPageResponse
        {
            Total = total,
            Items = items.Select(p => StoreProductMapper.ToResponse(
                p, stock is null ? null : stock.GetValueOrDefault(p.Id))).ToList(),
        };
        var suppliers = await _trade.GetLastSupplierNamesAsync(companyId, items.Select(i => i.Id).ToList(), ct);
        foreach (var item in result.Items)
            item.SupplierName = suppliers.GetValueOrDefault(item.Id);
        await _pricing.ApplyAsync(result.Items, query.WarehouseId, ct);
        return result;
    }

    public async Task<StoreProductResponse> Handle(FindStoreProductByCodeQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var code = (query.Code ?? string.Empty).Trim();
        if (code.Length == 0)
            throw new BadRequestException("Barkod boşdur.");

        var product = await _repository.FindProductByCodeAsync(companyId, code, ct);
        decimal? scanned = null;

        // A weighed-goods label: 13 digits, "2" + 5-digit item code + 5-digit weight (grams / millilitres) + check digit.
        if (product is null && code.Length == 13 && code[0] == '2' && code.All(char.IsDigit))
        {
            var item = code.Substring(1, 5);
            var amount = decimal.Parse(code.Substring(6, 5));
            product = await _repository.FindProductByCodeAsync(companyId, item, ct)
                ?? await _repository.FindProductByCodeAsync(companyId, item.TrimStart('0'), ct);
            if (product is not null)
                scanned = product.Unit is Domain.Enums.UnitOfMeasure.Kg or Domain.Enums.UnitOfMeasure.Liter ? amount / 1000m : amount;
        }

        if (product is null)
            throw new NotFoundException("Bu barkodla aktiv məhsul tapılmadı.");

        decimal? stock = null;
        if (query.WarehouseId is { } warehouseId)
            stock = (await _repository.GetWarehouseQuantitiesAsync(companyId, warehouseId, new[] { product.Id }, ct))
                .GetValueOrDefault(product.Id);

        var response = StoreProductMapper.ToResponse(product, stock);
        response.ScannedQuantity = scanned;
        await _pricing.ApplyAsync(new[] { response }, query.WarehouseId, ct);
        return response;
    }

    public async Task<StoreProductResponse> Handle(GetStoreProductQuery query, CancellationToken ct)
    {
        var product = await _repository.GetProductAsync(query.Id, _currentUser.CompanyId, ct)
            ?? throw new NotFoundException("Məhsul tapılmadı.");
        var response = StoreProductMapper.ToResponse(product);
        await _pricing.ApplyAsync(new[] { response }, query.WarehouseId, ct);
        return response;
    }

    public async Task<StoreProductResponse> Handle(SaveStoreProductCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BadRequestException("Məhsulun adı boş ola bilməz.");
        if (!Enum.IsDefined(dto.Unit))
            throw new BadRequestException("Ölçü vahidi yanlışdır.");
        if (dto.PurchasePrice < 0 || dto.SalePrice < 0 || dto.WholesalePrice < 0)
            throw new BadRequestException("Qiymət mənfi ola bilməz.");
        if (dto.VatPercent is < 0 or > 100)
            throw new BadRequestException("ƏDV faizi 0 ilə 100 arasında olmalıdır.");
        if (dto.MinimumStock < 0)
            throw new BadRequestException("Minimum stok mənfi ola bilməz.");

        if (dto.CategoryId is { } categoryId && await _repository.GetCategoryAsync(categoryId, companyId, ct) is null)
            throw new BadRequestException("Kateqoriya tapılmadı.");

        var codes = (dto.Barcodes ?? new()).Select(c => c?.Trim() ?? "").Where(c => c.Length > 0).ToList();
        if (codes.Count != codes.Distinct().Count())
            throw new BadRequestException("Eyni barkod iki dəfə yazılıb.");

        var sku = string.IsNullOrWhiteSpace(dto.Sku) ? null : dto.Sku.Trim();

        StoreProduct product;
        if (command.Id is { } id)
        {
            product = await _repository.GetProductAsync(id, companyId, ct)
                ?? throw new NotFoundException("Məhsul tapılmadı.");
        }
        else
        {
            product = new StoreProduct { CompanyId = companyId, CreatedByUserId = _currentUser.UserId };
        }

        if (command.Id is not null && product.TracksSerial != dto.TracksSerial
            && await _repository.GetTotalQuantityAsync(companyId, product.Id, ct) != 0)
            throw new BadRequestException("Anbarda qalığı olan məhsulun IMEI/serial izləməsini dəyişmək olmaz; əvvəlcə stoku sıfırlayın.");

        foreach (var code in codes)
            if (await _repository.BarcodeTakenAsync(companyId, code, command.Id, ct))
                throw new BadRequestException($"\"{code}\" barkodu başqa məhsulda var.");
        if (sku is not null && await _repository.SkuTakenAsync(companyId, sku, command.Id, ct))
            throw new BadRequestException($"\"{sku}\" SKU-su başqa məhsulda var.");

        var oldSale = product.SalePrice;
        var oldPurchase = product.PurchasePrice;
        var oldWholesale = product.WholesalePrice;

        product.Name = dto.Name.Trim();
        product.Sku = sku;
        product.CategoryId = dto.CategoryId;
        product.Brand = string.IsNullOrWhiteSpace(dto.Brand) ? null : dto.Brand.Trim();
        product.Unit = dto.Unit;
        product.PurchasePrice = Math.Round(dto.PurchasePrice, 2, MidpointRounding.AwayFromZero);
        product.SalePrice = Math.Round(dto.SalePrice, 2, MidpointRounding.AwayFromZero);
        product.WholesalePrice = dto.WholesalePrice is { } w ? Math.Round(w, 2, MidpointRounding.AwayFromZero) : null;
        product.VatPercent = dto.VatPercent;
        if (dto.CampaignPrice is < 0)
            throw new BadRequestException("Kampaniya qiyməti mənfi ola bilməz.");
        if (dto.CampaignFromUtc is { } cf && dto.CampaignToUtc is { } ct2 && ct2 < cf)
            throw new BadRequestException("Kampaniyanın sonu başlanğıcdan əvvəl ola bilməz.");
        product.CampaignPrice = dto.CampaignPrice is { } cp ? Math.Round(cp, 2, MidpointRounding.AwayFromZero) : null;
        product.CampaignFromUtc = dto.CampaignPrice is null ? null : dto.CampaignFromUtc;
        product.CampaignToUtc = dto.CampaignPrice is null ? null : dto.CampaignToUtc;
        product.MinimumStock = dto.MinimumStock;
        product.TracksSerial = dto.TracksSerial;
        product.EarnsBonus = dto.EarnsBonus;
        product.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        product.IsActive = dto.IsActive;

        // Every price change is kept: what it was, what it became, when and by whom.
        async Task Record(StorePriceType type, decimal? oldPrice, decimal? newPrice)
        {
            if (newPrice is null || oldPrice == newPrice) return;
            await _trade.AddPriceHistoryAsync(new StorePriceHistory
            {
                CompanyId = companyId,
                StoreProduct = product,
                PriceType = type,
                OldPrice = command.Id is null ? null : oldPrice,
                NewPrice = newPrice.Value,
                CreatedByUserId = _currentUser.UserId,
            }, ct);
        }
        await Record(StorePriceType.Sale, command.Id is null ? null : oldSale, product.SalePrice);
        await Record(StorePriceType.Purchase, command.Id is null ? null : oldPurchase, product.PurchasePrice);
        await Record(StorePriceType.Wholesale, command.Id is null ? null : oldWholesale, product.WholesalePrice);

        if (command.Id is null)
        {
            // A product with no stock history costs what it was bought for.
            product.AverageCost = product.PurchasePrice;
            await _repository.AddProductAsync(product, ct);
        }

        // Replace the codes: keep those still listed, drop the others, add the new ones.
        foreach (var gone in product.Barcodes.Where(b => !codes.Contains(b.Barcode)).ToList())
            product.Barcodes.Remove(gone);
        foreach (var code in codes.Where(c => product.Barcodes.All(b => b.Barcode != c)))
            product.Barcodes.Add(new StoreProductBarcode { CompanyId = companyId, Barcode = code, CreatedByUserId = _currentUser.UserId });
        foreach (var b in product.Barcodes)
            b.IsPrimary = codes.Count > 0 && b.Barcode == codes[0];

        await _repository.SaveChangesAsync(ct);
        return StoreProductMapper.ToResponse(product);
    }
}
