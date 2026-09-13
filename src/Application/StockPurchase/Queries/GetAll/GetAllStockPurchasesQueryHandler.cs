using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockPurchase.Dtos.Response;
using MediatR;

namespace Application.StockPurchase.Queries.GetAll;

public class GetAllStockPurchasesQueryHandler
    : IRequestHandler<GetAllStockPurchasesQuery, BaseResponse<List<StockPurchaseResponse>>>
{
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllStockPurchasesQueryHandler(
        IStockPurchaseRepository purchaseRepository,
        ICurrentUserService currentUserService)
    {
        _purchaseRepository = purchaseRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<List<StockPurchaseResponse>>> Handle(
        GetAllStockPurchasesQuery request, CancellationToken cancellationToken)
    {
        var purchases = await _purchaseRepository.GetAllAsync(cancellationToken);

        // A tenant Admin only ever sees their own company's purchase documents. SuperAdmin can
        // target one company via CompanyId, or omit it to browse every company.
        if (!_currentUserService.IsSuperAdmin)
        {
            purchases = purchases.Where(p => p.CompanyId == _currentUserService.CompanyId).ToList();
        }
        else if (request.CompanyId is > 0)
        {
            purchases = purchases.Where(p => p.CompanyId == request.CompanyId.Value).ToList();
        }

        var result = purchases.Select(p => MapToResponse(p)).ToList();

        return new BaseResponse<List<StockPurchaseResponse>> { Success = true, Data = result };
    }

    private static StockPurchaseResponse MapToResponse(Domain.Entities.WarehouseAndStock.StockPurchase p)
    {
        var lines = p.Lines.Select(l => new StockPurchaseLineResponse
        {
            Id = l.Id,
            StockItemId = l.StockItemId,
            StockItemName = l.StockItem?.Name ?? string.Empty,
            Quantity = l.Quantity,
            UnitPriceForeign = l.UnitPriceForeign,
            UnitPriceAzn = l.UnitPriceAzn,
            TotalForeign = l.Quantity * l.UnitPriceForeign,
            TotalAzn = l.Quantity * l.UnitPriceAzn,
        }).ToList();

        return new StockPurchaseResponse
        {
            Id = p.Id,
            DocumentNo = p.DocumentNo,
            CompanyId = p.CompanyId,
            CounterpartyId = p.CounterpartyId,
            CounterpartyName = p.Counterparty?.Name ?? "",
            IsImport = p.IsImport,
            Currency = p.Currency,
            CurrencyCode = p.Currency.ToString(),
            ExchangeRate = p.ExchangeRate,
            PurchaseDate = p.PurchaseDate,
            Status = p.Status,
            WarehouseId = p.WarehouseId,
            WarehouseName = p.Warehouse?.Name ?? string.Empty,
            Note = p.Note,
            CreatedAtUtc = p.CreatedAtUtc,
            TotalForeign = lines.Sum(l => l.TotalForeign),
            TotalAzn = lines.Sum(l => l.TotalAzn),
            Lines = lines,
        };
    }
}
