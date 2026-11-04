using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.StoreMode.Access;
using MediatR;

namespace Application.StoreMode.Pricing;

public interface IStorePricingRepository
{
    Task<Dictionary<int, decimal>> GetBranchPricesAsync(int companyId, int branchId, IReadOnlyCollection<int> productIds, CancellationToken ct);
    Task<List<(int BranchId, decimal SalePrice)>> GetProductBranchPricesAsync(int companyId, int productId, CancellationToken ct);
    Task ReplaceProductBranchPricesAsync(int companyId, int productId, IReadOnlyCollection<(int BranchId, decimal SalePrice)> prices, CancellationToken ct);
}

/// <summary>
/// The price list per branch. What the till charges: the running campaign price if there is one, otherwise the branch's own
/// price for the product if it has one, otherwise the product's normal sale price.
/// </summary>
public sealed class StorePricingService
{
    private readonly IStorePricingRepository _repository;
    private readonly IStoreAccessRepository _access;
    private readonly ICurrentUserService _currentUser;

    public StorePricingService(IStorePricingRepository repository, IStoreAccessRepository access, ICurrentUserService currentUser)
    {
        _repository = repository;
        _access = access;
        _currentUser = currentUser;
    }

    public static decimal Effective(decimal salePrice, decimal? campaignPrice, DateTime? from, DateTime? to, decimal? branchPrice, DateTime nowUtc)
        => campaignPrice is { } c && (from is null || from <= nowUtc) && (to is null || to >= nowUtc) ? c : branchPrice ?? salePrice;

    public Task<Dictionary<int, decimal>> BranchPricesAsync(int branchId, IReadOnlyCollection<int> productIds, CancellationToken ct)
        => _repository.GetBranchPricesAsync(_currentUser.CompanyId, branchId, productIds, ct);

    /// <summary>Sets <c>EffectivePrice</c> on the products as the till of this warehouse's branch would charge them.</summary>
    public async Task ApplyAsync(IReadOnlyCollection<StoreProductResponse> items, int? warehouseId, CancellationToken ct)
    {
        Dictionary<int, decimal>? branch = null;
        if (warehouseId is { } wid && items.Count > 0
            && await _access.GetWarehouseBranchAsync(_currentUser.CompanyId, wid, ct) is { } branchId)
            branch = await BranchPricesAsync(branchId, items.Select(i => i.Id).ToList(), ct);

        var now = DateTime.UtcNow;
        foreach (var i in items)
            i.EffectivePrice = Effective(i.SalePrice, i.CampaignPrice, i.CampaignFromUtc, i.CampaignToUtc, branch?.GetValueOrDefault(i.Id) is { } p and > 0 ? p : null, now);
    }
}

public class StoreBranchPriceItem
{
    public int BranchId { get; set; }
    public decimal SalePrice { get; set; }
}

public class SaveStoreBranchPricesRequest
{
    /// <summary>The full list for the product; a branch not listed uses the normal sale price.</summary>
    public List<StoreBranchPriceItem> Prices { get; set; } = new();
}

public record GetStoreBranchPricesQuery(int ProductId) : IRequest<List<StoreBranchPriceItem>>;

public record SaveStoreBranchPricesCommand(int ProductId, SaveStoreBranchPricesRequest Request) : IRequest<List<StoreBranchPriceItem>>;

public class StoreBranchPriceHandlers :
    IRequestHandler<GetStoreBranchPricesQuery, List<StoreBranchPriceItem>>,
    IRequestHandler<SaveStoreBranchPricesCommand, List<StoreBranchPriceItem>>
{
    private readonly IStorePricingRepository _repository;
    private readonly IStoreRepository _store;
    private readonly ICurrentUserService _currentUser;

    public StoreBranchPriceHandlers(IStorePricingRepository repository, IStoreRepository store, ICurrentUserService currentUser)
    {
        _repository = repository;
        _store = store;
        _currentUser = currentUser;
    }

    public async Task<List<StoreBranchPriceItem>> Handle(GetStoreBranchPricesQuery query, CancellationToken ct)
        => (await _repository.GetProductBranchPricesAsync(_currentUser.CompanyId, query.ProductId, ct))
            .Select(x => new StoreBranchPriceItem { BranchId = x.BranchId, SalePrice = x.SalePrice }).ToList();

    public async Task<List<StoreBranchPriceItem>> Handle(SaveStoreBranchPricesCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        _ = await _store.GetProductAsync(command.ProductId, companyId, ct) ?? throw new NotFoundException("Məhsul tapılmadı.");

        var items = command.Request.Prices;
        if (items.Select(i => i.BranchId).Distinct().Count() != items.Count)
            throw new BadRequestException("Eyni filial iki dəfə yazılıb.");
        foreach (var i in items)
        {
            if (i.SalePrice <= 0)
                throw new BadRequestException("Filial qiyməti 0-dan böyük olmalıdır (qiymət boşdursa sətri silin).");
            if (!await _store.BranchExistsAsync(companyId, i.BranchId, ct))
                throw new BadRequestException("Filial tapılmadı.");
        }

        await _repository.ReplaceProductBranchPricesAsync(
            companyId, command.ProductId, items.Select(i => (i.BranchId, Math.Round(i.SalePrice, 2, MidpointRounding.AwayFromZero))).ToList(), ct);
        return items;
    }
}
