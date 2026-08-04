using Application.Common.Responce;
using MediatR;

namespace Application.StockPurchase.Queries.GetExchangeRate;

public record GetCbarExchangeRateQuery(string CurrencyCode, DateTime Date)
    : IRequest<BaseResponse<decimal>>;
