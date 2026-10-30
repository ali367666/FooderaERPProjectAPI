using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using MediatR;

namespace Application.StockWriteOffs.Queries;

/// <param name="From">First day (inclusive); null = no lower bound.</param>
/// <param name="To">Last day (inclusive); null = no upper bound.</param>
public record GetStockWriteOffsQuery(DateTime? From, DateTime? To, int? WarehouseId) : IRequest<StockWriteOffListResponse>;

public class GetStockWriteOffsQueryHandler : IRequestHandler<GetStockWriteOffsQuery, StockWriteOffListResponse>
{
    private readonly IStockWriteOffRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetStockWriteOffsQueryHandler(IStockWriteOffRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<StockWriteOffListResponse> Handle(GetStockWriteOffsQuery query, CancellationToken cancellationToken)
    {
        // Days follow the same business-day cut-off as the other reports.
        DateTime? fromUtc = query.From is { } f ? BusinessTime.ToUtc(f.Date + ReportPeriodResolver.DayStart) : null;
        DateTime? toUtc = query.To is { } t ? BusinessTime.ToUtc(t.Date.AddDays(1) + ReportPeriodResolver.DayStart) : null;

        var rows = await _repository.SearchAsync(
            _currentUserService.CompanyId, fromUtc, toUtc, query.WarehouseId, cancellationToken);

        var names = await _repository.GetUserNamesAsync(
            rows.Select(r => r.CreatedByUserId).OfType<int>(), cancellationToken);

        var items = rows.Select(r => new StockWriteOffResponse
        {
            Id = r.Id,
            WrittenOffAtUtc = r.CreatedAtUtc,
            WrittenOffByUserId = r.CreatedByUserId,
            WrittenOffByUserName = r.CreatedByUserId is { } id && names.TryGetValue(id, out var n) ? n : null,
            WarehouseId = r.WarehouseId,
            WarehouseName = r.Warehouse?.Name ?? string.Empty,
            StockItemId = r.StockItemId,
            StockItemName = r.StockItem?.Name ?? string.Empty,
            Quantity = r.Quantity,
            UnitCostAzn = r.UnitCostAzn,
            LossAzn = Math.Round(r.Quantity * r.UnitCostAzn, 2, MidpointRounding.AwayFromZero),
            Reason = r.Reason,
            ReasonName = r.Reason.ToString(),
            Note = r.Note,
            StockPurchaseId = r.StockPurchaseId,
            PurchaseDocumentNo = r.StockPurchase?.DocumentNo,
        }).ToList();

        return new StockWriteOffListResponse { TotalLossAzn = items.Sum(i => i.LossAzn), Items = items };
    }
}
