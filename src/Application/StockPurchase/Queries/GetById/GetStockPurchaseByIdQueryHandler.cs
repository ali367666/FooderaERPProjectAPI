using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockPurchase.Dtos.Response;
using Application.StockPurchase.Queries.GetAll;
using MediatR;

namespace Application.StockPurchase.Queries.GetById;

public class GetStockPurchaseByIdQueryHandler
    : IRequestHandler<GetStockPurchaseByIdQuery, BaseResponse<StockPurchaseResponse>>
{
    private readonly IStockPurchaseRepository _purchaseRepository;

    public GetStockPurchaseByIdQueryHandler(IStockPurchaseRepository purchaseRepository)
    {
        _purchaseRepository = purchaseRepository;
    }

    public async Task<BaseResponse<StockPurchaseResponse>> Handle(
        GetStockPurchaseByIdQuery request, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (purchase is null)
            return new BaseResponse<StockPurchaseResponse> { Success = false, Message = "Stok alışı tapılmadı." };

        var lines = purchase.Lines.Select(l => new StockPurchaseLineResponse
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

        var response = new StockPurchaseResponse
        {
            Id = purchase.Id,
            DocumentNo = purchase.DocumentNo,
            CompanyId = purchase.CompanyId,
            CounterpartyId = purchase.CounterpartyId,
            CounterpartyName = purchase.Counterparty?.Name ?? "",
            IsImport = purchase.IsImport,
            Currency = purchase.Currency,
            CurrencyCode = purchase.Currency.ToString(),
            ExchangeRate = purchase.ExchangeRate,
            PurchaseDate = purchase.PurchaseDate,
            Status = purchase.Status,
            WarehouseId = purchase.WarehouseId,
            WarehouseName = purchase.Warehouse?.Name ?? string.Empty,
            Note = purchase.Note,
            CreatedAtUtc = purchase.CreatedAtUtc,
            TotalForeign = lines.Sum(l => l.TotalForeign),
            TotalAzn = lines.Sum(l => l.TotalAzn),
            Lines = lines,
        };

        return new BaseResponse<StockPurchaseResponse> { Success = true, Data = response };
    }
}
