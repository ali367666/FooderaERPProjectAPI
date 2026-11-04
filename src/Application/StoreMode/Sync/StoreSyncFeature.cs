using System.Text.Json;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.StoreMode.Catalog;
using Application.StoreMode.Notifications;
using Domain.Constants;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.StoreMode.Sync;

public class StoreSyncOperationRequest
{
    /// <summary>The operation's own id (for a sale: the sale's OperationId); the server never handles the same one twice.</summary>
    public Guid OperationId { get; set; }

    /// <summary>"Sale".</summary>
    public string Type { get; set; } = "Sale";

    /// <summary>When the till made it (its clock).</summary>
    public DateTime? CreatedAtUtc { get; set; }

    public JsonElement Payload { get; set; }
}

public class StoreSyncPushRequest
{
    public string DeviceId { get; set; } = default!;
    public List<StoreSyncOperationRequest> Operations { get; set; } = new();
}

public class StoreSyncOperationResult
{
    public Guid OperationId { get; set; }
    public StoreSyncStatus Status { get; set; }
    public string? DocumentNo { get; set; }
    public int? EntityId { get; set; }
    public string? Error { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? DeviceId { get; set; }
    public string? Type { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class StoreSyncPullResponse
{
    public DateTime ServerTimeUtc { get; set; }
    public StoreSettingsResponse Settings { get; set; } = default!;
    public List<StoreProductResponse> Products { get; set; } = new();
}

/// <summary>What a till device sends when it is back online: its waiting operations, each handled once and answered separately.</summary>
public record PushStoreSyncCommand(StoreSyncPushRequest Request) : IRequest<List<StoreSyncOperationResult>>;

public record GetStoreSyncOperationsQuery(StoreSyncStatus? Status, string? DeviceId) : IRequest<List<StoreSyncOperationResult>>;

/// <summary>What a till keeps for offline work: active products with prices, barcodes and the warehouse's stock, plus the rules.</summary>
public record PullStoreSyncQuery(int? WarehouseId) : IRequest<StoreSyncPullResponse>;

public class StoreSyncHandlers :
    IRequestHandler<PushStoreSyncCommand, List<StoreSyncOperationResult>>,
    IRequestHandler<GetStoreSyncOperationsQuery, List<StoreSyncOperationResult>>,
    IRequestHandler<PullStoreSyncQuery, StoreSyncPullResponse>
{
    private const int MaxOperations = 100;
    private const int MaxProducts = 20_000;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IStoreSyncRepository _sync;
    private readonly IStoreRepository _store;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceScopeFactory _scopes;
    private readonly StoreNotifier _notifier;
    private readonly Application.StoreMode.Pricing.StorePricingService _pricing;
    private readonly ILogger<StoreSyncHandlers> _logger;

    public StoreSyncHandlers(
        IStoreSyncRepository sync,
        IStoreRepository store,
        ICurrentUserService currentUser,
        IServiceScopeFactory scopes,
        StoreNotifier notifier,
        ILogger<StoreSyncHandlers> logger,
        Application.StoreMode.Pricing.StorePricingService pricing)
    {
        _pricing = pricing;
        _sync = sync;
        _store = store;
        _currentUser = currentUser;
        _scopes = scopes;
        _notifier = notifier;
        _logger = logger;
    }

    private static StoreSyncOperationResult Map(StoreSyncOperation o) => new()
    {
        OperationId = o.OperationId,
        Status = o.Status,
        DocumentNo = o.ResultDocumentNo,
        EntityId = o.ResultEntityId,
        Error = o.Error,
        AttemptCount = o.AttemptCount,
        ProcessedAtUtc = o.ProcessedAtUtc,
        DeviceId = o.DeviceId,
        Type = o.Type,
        CreatedAtUtc = o.CreatedAtUtc,
    };

    public async Task<List<StoreSyncOperationResult>> Handle(PushStoreSyncCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var dto = command.Request;
        if (string.IsNullOrWhiteSpace(dto.DeviceId))
            throw new BadRequestException("Cihaz identifikatoru boşdur.");
        if (dto.Operations.Count > MaxOperations)
            throw new BadRequestException($"Bir dəfəyə ən çox {MaxOperations} əməliyyat göndərilə bilər.");

        var results = new List<StoreSyncOperationResult>();
        foreach (var op in dto.Operations)
        {
            var record = await _sync.GetByOperationAsync(companyId, op.OperationId, ct);
            if (record is { Status: StoreSyncStatus.Synced })
            {
                // Already done: answer the same, change nothing.
                results.Add(Map(record));
                continue;
            }

            if (record is null)
            {
                record = new StoreSyncOperation
                {
                    CompanyId = companyId,
                    OperationId = op.OperationId,
                    DeviceId = dto.DeviceId.Trim(),
                    Type = op.Type,
                    ClientCreatedAtUtc = op.CreatedAtUtc,
                    CreatedByUserId = _currentUser.UserId,
                };
                await _sync.AddAsync(record, ct);
            }

            record.AttemptCount++;
            record.Status = StoreSyncStatus.Syncing;

            try
            {
                switch (op.Type)
                {
                    case "Sale":
                        var request = op.Payload.Deserialize<CreateStoreSaleRequest>(Json)
                            ?? throw new BadRequestException("Satışın məlumatı oxunmadı.");
                        request.OperationId = op.OperationId;
                        request.Approval = null; // a manager's password is never kept on a device
                        // Its own scope: a refused sale must leave nothing half-tracked behind for the next one.
                        using (var scope = _scopes.CreateScope())
                        {
                            var sale = await scope.ServiceProvider.GetRequiredService<IMediator>()
                                .Send(new Sales.CreateStoreSaleCommand(request), ct);
                            record.ResultDocumentNo = sale.DocumentNo;
                            record.ResultEntityId = sale.Id;
                        }
                        break;
                    default:
                        throw new BadRequestException($"Əməliyyat növü dəstəklənmir: {op.Type}");
                }

                record.Status = StoreSyncStatus.Synced;
                record.Error = null;
            }
            catch (BadRequestException ex)
            {
                // Stock or serial gone: a person has to decide (adjust stock, or cancel on the till's side).
                var needsPerson = ex.Message.Contains("anbarda kifayət qədər yoxdur") || ex.Message.Contains("satışa hazır deyil") || ex.Message.Contains("tapılmadı");
                record.Status = needsPerson ? StoreSyncStatus.Conflict : StoreSyncStatus.Failed;
                record.Error = Trim(ex.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Sync operation {OperationId} failed", op.OperationId);
                record.Status = StoreSyncStatus.Failed;
                record.Error = "Gözlənilməyən xəta; yenidən cəhd edin.";
            }

            record.ProcessedAtUtc = DateTime.UtcNow;
            await _sync.SaveChangesAsync(ct);

            if (record.Status is StoreSyncStatus.Conflict or StoreSyncStatus.Failed)
            {
                _logger.LogWarning("Sync {Status}: {OperationId} from {Device}: {Error}", record.Status, record.OperationId, record.DeviceId, record.Error);
                await _notifier.NotifyHoldersAsync(
                    AppPermissions.StoreReportView,
                    "Sinxronizasiya xətası",
                    $"{record.DeviceId} cihazından gələn satış qəbul edilmədi: {record.Error}",
                    "StoreSyncError", record.Id, "StoreSyncOperation", ct);
            }

            results.Add(Map(record));
        }

        return results;
    }

    private static string? Trim(string? v) => v is null ? null : v.Length <= 1000 ? v : v[..1000];

    public async Task<List<StoreSyncOperationResult>> Handle(GetStoreSyncOperationsQuery query, CancellationToken ct)
        => (await _sync.SearchAsync(_currentUser.CompanyId, query.Status, string.IsNullOrWhiteSpace(query.DeviceId) ? null : query.DeviceId, 200, ct))
            .Select(Map).ToList();

    public async Task<StoreSyncPullResponse> Handle(PullStoreSyncQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var settings = await _store.GetOrCreateSettingsAsync(companyId, ct);
        var response = new StoreSyncPullResponse
        {
            ServerTimeUtc = DateTime.UtcNow,
            Settings = new StoreSettingsResponse
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
            },
        };

        for (var skip = 0; skip < MaxProducts; skip += 500)
        {
            var (items, _) = await _store.SearchProductsAsync(companyId, null, null, true, skip, 500, ct);
            if (items.Count == 0) break;
            Dictionary<int, decimal>? stock = null;
            if (query.WarehouseId is { } warehouseId)
                stock = await _store.GetWarehouseQuantitiesAsync(companyId, warehouseId, items.Select(i => i.Id).ToList(), ct);
            var batch = items.Select(p => StoreProductMapper.ToResponse(p, stock?.GetValueOrDefault(p.Id))).ToList();
            await _pricing.ApplyAsync(batch, query.WarehouseId, ct);
            response.Products.AddRange(batch);
            if (items.Count < 500) break;
        }

        return response;
    }
}
