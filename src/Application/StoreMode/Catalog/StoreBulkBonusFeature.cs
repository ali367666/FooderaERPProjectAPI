using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.StoreMode.Catalog;

public class SetStoreProductsBonusRequest
{
    /// <summary>The products to change. Left out, every product matching <see cref="Term"/> and <see cref="CategoryId"/> is changed.</summary>
    public List<int>? ProductIds { get; set; }

    public string? Term { get; set; }
    public int? CategoryId { get; set; }

    /// <summary>False: these products earn no bonus. True: they earn it again.</summary>
    public bool EarnsBonus { get; set; }
}

public class SetStoreProductsBonusResponse
{
    public int Changed { get; set; }
}

/// <summary>Switches bonus on or off for many products at once.</summary>
public record SetStoreProductsBonusCommand(SetStoreProductsBonusRequest Request) : IRequest<SetStoreProductsBonusResponse>;

public class SetStoreProductsBonusHandler : IRequestHandler<SetStoreProductsBonusCommand, SetStoreProductsBonusResponse>
{
    private const int MaxProducts = 5000;

    private readonly IStoreRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public SetStoreProductsBonusHandler(IStoreRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<SetStoreProductsBonusResponse> Handle(SetStoreProductsBonusCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;

        List<Domain.Entities.StoreMode.StoreProduct> products;
        if (dto.ProductIds is { Count: > 0 } ids)
        {
            if (ids.Count > MaxProducts)
                throw new BadRequestException($"Bir dəfəyə ən çox {MaxProducts} məhsul dəyişilə bilər.");
            products = await _repository.GetProductsByIdsAsync(companyId, ids, ct);
        }
        else if (dto.ProductIds is null && (dto.CategoryId is not null || !string.IsNullOrWhiteSpace(dto.Term)))
        {
            // Everything the list shows for this search / category, not only the visible page.
            (products, _) = await _repository.SearchProductsAsync(companyId, dto.Term, dto.CategoryId, null, 0, MaxProducts, ct);
        }
        else
        {
            throw new BadRequestException("Məhsul seçilməyib.");
        }

        foreach (var p in products)
            p.EarnsBonus = dto.EarnsBonus;
        await _repository.SaveChangesAsync(ct);

        return new SetStoreProductsBonusResponse { Changed = products.Count };
    }
}
