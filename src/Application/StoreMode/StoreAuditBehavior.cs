using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.StoreMode.Catalog;
using Application.StoreMode.Stock;
using Application.Common.Models;
using MediatR;

namespace Application.StoreMode;

/// <summary>
/// Writes every store command that went through (sale, cancel, return, stock adjustment, shift, payment, price
/// change...) to the audit log: who, what, which document and the request without secrets. Reads are not logged.
/// A failed command changes nothing, so it leaves no entry; the audit write itself never breaks the operation.
/// </summary>
public sealed class StoreAuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly bool Audited =
        typeof(TRequest).Namespace?.StartsWith("Application.StoreMode", StringComparison.Ordinal) == true
        && typeof(TRequest).Name.EndsWith("Command", StringComparison.Ordinal);

    private static readonly string[] Secrets = { "approval", "password", "managerPassword", "csv" };

    private readonly IAuditLogService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly IStoreRepository _store;

    public StoreAuditBehavior(IAuditLogService audit, ICurrentUserService currentUser, IStoreRepository store)
    {
        _store = store;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!Audited) return await next();

        var before = await SnapshotAsync(request, ct);
        var response = await next();

        try
        {
            var name = typeof(TRequest).Name[..^"Command".Length];
            var node = JsonSerializer.SerializeToNode(request, request.GetType());
            Redact(node);

            await _audit.LogAsync(new AuditLogEntry
            {
                EntityName = "Store" + name.Replace("Store", string.Empty),
                EntityId = ReadId(request, response),
                ActionType = name,
                OldValues = before,
                NewValues = node?.ToJsonString(),
                Message = $"Mağaza əməliyyatı: {name}",
                UserId = _currentUser.UserId,
                CompanyId = _currentUser.CompanyId,
                IsSuccess = true,
            }, CancellationToken.None);
        }
        catch
        {
            // the audit trail must never undo a finished sale
        }

        return response;
    }

    /// <summary>The state a command is about to overwrite, for the commands where "what was it before" matters (prices, rules).</summary>
    private async Task<string?> SnapshotAsync(TRequest request, CancellationToken ct)
    {
        try
        {
            switch (request)
            {
                case SaveStoreProductCommand { Id: { } id }:
                    var p = await _store.GetProductAsync(id, _currentUser.CompanyId, ct);
                    return p is null ? null : JsonSerializer.Serialize(new
                    {
                        p.Name, p.Sku, p.PurchasePrice, p.SalePrice, p.WholesalePrice, p.CampaignPrice, p.CampaignFromUtc, p.CampaignToUtc,
                        p.VatPercent, p.MinimumStock, p.IsActive, p.EarnsBonus,
                    });
                case UpdateStoreSettingsCommand:
                    var s = await _store.GetOrCreateSettingsAsync(_currentUser.CompanyId, ct);
                    return JsonSerializer.Serialize(new
                    {
                        s.AllowNegativeStock, s.MaxDiscountPercent, s.BonusEarnPercent, s.BonusMaxPaymentPercent, s.BonusExpiryDays, s.FiscalEnabled,
                    });
                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }

    private static string ReadId(TRequest request, TResponse response)
    {
        foreach (var source in new object?[] { response, request })
        {
            var id = source?.GetType().GetProperty("Id")?.GetValue(source);
            if (id is not null) return id.ToString()!;
        }
        return "0";
    }

    private static void Redact(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    if (Secrets.Contains(key, StringComparer.OrdinalIgnoreCase)) obj[key] = "***";
                    else Redact(obj[key]);
                }
                break;
            case JsonArray arr:
                foreach (var item in arr) Redact(item);
                break;
        }
    }
}
