using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.StoreMode.Access;

public interface IStoreAccessRepository
{
    Task<List<int>> GetBranchIdsAsync(int companyId, int userId, CancellationToken ct);
    Task ReplaceBranchIdsAsync(int companyId, int userId, IReadOnlyCollection<int> branchIds, CancellationToken ct);
    Task<int?> GetWarehouseBranchAsync(int companyId, int warehouseId, CancellationToken ct);
    Task<List<int>> GetWarehouseIdsInBranchesAsync(int companyId, IReadOnlyCollection<int> branchIds, CancellationToken ct);
}

/// <summary>
/// Which branches the current user may work in. A user with no branch rows is not restricted (the default, so existing users
/// keep working); with rows, the store's registers, shifts, sales, stock and reports are limited to those branches.
/// The platform SuperAdmin is never restricted.
/// </summary>
public sealed class StoreAccessService
{
    private readonly IStoreAccessRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private List<int>? _cache;
    private bool _loaded;

    public StoreAccessService(IStoreAccessRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <summary>The branches the user is limited to, or null when not limited.</summary>
    public async Task<IReadOnlyCollection<int>?> AllowedBranchesAsync(CancellationToken ct)
    {
        if (!_loaded)
        {
            _loaded = true;
            if (!_currentUser.IsSuperAdmin)
            {
                var ids = await _repository.GetBranchIdsAsync(_currentUser.CompanyId, _currentUser.UserId, ct);
                _cache = ids.Count == 0 ? null : ids;
            }
        }
        return _cache;
    }

    public async Task<bool> CanUseBranchAsync(int branchId, CancellationToken ct)
        => await AllowedBranchesAsync(ct) is not { } allowed || allowed.Contains(branchId);

    public async Task EnsureBranchAsync(int branchId, CancellationToken ct)
    {
        if (!await CanUseBranchAsync(branchId, ct))
            throw new BadRequestException("Bu filialda işləməyə icazəniz yoxdur.");
    }

    public async Task EnsureWarehouseAsync(int warehouseId, CancellationToken ct)
    {
        if (await AllowedBranchesAsync(ct) is null) return;
        var branch = await _repository.GetWarehouseBranchAsync(_currentUser.CompanyId, warehouseId, ct);
        if (branch is null || !await CanUseBranchAsync(branch.Value, ct))
            throw new BadRequestException("Bu anbara icazəniz yoxdur.");
    }

    /// <summary>At least one of the warehouses must be the user's (a transfer is worked on from either end).</summary>
    public async Task EnsureAnyWarehouseAsync(CancellationToken ct, params int[] warehouseIds)
    {
        if (await AllowedWarehousesAsync(ct) is not { } allowed) return;
        if (!warehouseIds.Any(allowed.Contains))
            throw new BadRequestException("Bu anbarlara icazəniz yoxdur.");
    }

    /// <summary>The warehouses of the allowed branches, or null when not limited.</summary>
    public async Task<HashSet<int>?> AllowedWarehousesAsync(CancellationToken ct)
        => await AllowedBranchesAsync(ct) is { } allowed
            ? (await _repository.GetWarehouseIdsInBranchesAsync(_currentUser.CompanyId, allowed, ct)).ToHashSet()
            : null;
}

public class StoreUserAccessResponse
{
    public int UserId { get; set; }

    /// <summary>Empty means the user may work in every branch.</summary>
    public List<int> BranchIds { get; set; } = new();
}

public class SaveStoreUserAccessRequest
{
    public List<int> BranchIds { get; set; } = new();
}

public record GetStoreUserAccessQuery(int UserId) : IRequest<StoreUserAccessResponse>;

public record SaveStoreUserAccessCommand(int UserId, SaveStoreUserAccessRequest Request) : IRequest<StoreUserAccessResponse>;

public class StoreUserAccessHandlers :
    IRequestHandler<GetStoreUserAccessQuery, StoreUserAccessResponse>,
    IRequestHandler<SaveStoreUserAccessCommand, StoreUserAccessResponse>
{
    private readonly IStoreAccessRepository _repository;
    private readonly IStoreRepository _store;
    private readonly ICurrentUserService _currentUser;

    public StoreUserAccessHandlers(IStoreAccessRepository repository, IStoreRepository store, ICurrentUserService currentUser)
    {
        _repository = repository;
        _store = store;
        _currentUser = currentUser;
    }

    public async Task<StoreUserAccessResponse> Handle(GetStoreUserAccessQuery query, CancellationToken ct)
        => new() { UserId = query.UserId, BranchIds = await _repository.GetBranchIdsAsync(_currentUser.CompanyId, query.UserId, ct) };

    public async Task<StoreUserAccessResponse> Handle(SaveStoreUserAccessCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var ids = command.Request.BranchIds.Distinct().ToList();
        foreach (var id in ids)
            if (!await _store.BranchExistsAsync(companyId, id, ct))
                throw new BadRequestException("Filial tapılmadı.");

        await _repository.ReplaceBranchIdsAsync(companyId, command.UserId, ids, ct);
        return new StoreUserAccessResponse { UserId = command.UserId, BranchIds = ids };
    }
}
