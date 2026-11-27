using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Reports;

/// <summary>The cost layers (deliveries still held) of the company's warehouse goods; there are none while it costs by the weighted average.</summary>
public record GetStoreCostLayersQuery(int? WarehouseId, int? ProductId, bool OnlyRemaining) : IRequest<StoreCostLayersResponse>;

public class StoreCostLayerResponse
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public DateTime ReceivedAtUtc { get; set; }
    public decimal OriginalQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }

    /// <summary>What is left of the layer times its cost (AZN).</summary>
    public decimal Value { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentNo { get; set; }
    public bool IsOpeningBalance { get; set; }
}

public class StoreCostLayersResponse
{
    /// <summary>0 = weighted average (no layers), 1 = FIFO, 2 = LIFO.</summary>
    public int Method { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalQuantity { get; set; }
    public List<StoreCostLayerResponse> Layers { get; set; } = new();
}

public class StoreCostLayersHandler : IRequestHandler<GetStoreCostLayersQuery, StoreCostLayersResponse>
{
    private readonly IStoreCostingRepository _costing;
    private readonly ICurrentUserService _currentUser;
    private readonly Application.StoreMode.Access.StoreAccessService _access;

    public StoreCostLayersHandler(IStoreCostingRepository costing, ICurrentUserService currentUser, Application.StoreMode.Access.StoreAccessService access)
    {
        _costing = costing;
        _currentUser = currentUser;
        _access = access;
    }

    public async Task<StoreCostLayersResponse> Handle(GetStoreCostLayersQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var method = await _costing.GetMethodAsync(companyId, ct);
        if (query.WarehouseId is { } wid) await _access.EnsureWarehouseAsync(wid, ct);

        var rows = await _costing.GetLayersAsync(companyId, query.WarehouseId, query.ProductId, query.OnlyRemaining, ct);
        if (await _access.AllowedWarehousesAsync(ct) is { } allowed)
            rows = rows.Where(r => allowed.Contains(r.WarehouseId)).ToList();

        var layers = rows.Select(r => new StoreCostLayerResponse
        {
            Id = r.Id,
            WarehouseId = r.WarehouseId,
            WarehouseName = r.WarehouseName,
            ProductId = r.ProductId,
            ProductName = r.ProductName,
            ReceivedAtUtc = r.ReceivedAtUtc,
            OriginalQuantity = r.OriginalQuantity,
            RemainingQuantity = r.RemainingQuantity,
            UnitCost = r.UnitCost,
            Value = Math.Round(r.RemainingQuantity * r.UnitCost, 2, MidpointRounding.AwayFromZero),
            DocumentType = r.DocumentType,
            DocumentNo = r.DocumentNo,
            IsOpeningBalance = r.IsOpeningBalance,
        }).ToList();

        return new StoreCostLayersResponse
        {
            Method = (int)method,
            TotalQuantity = layers.Sum(l => l.RemainingQuantity),
            TotalValue = layers.Sum(l => l.Value),
            Layers = layers,
        };
    }
}
