using Application.Common.Responce;
using Application.StockPurchase.Dtos.Response;
using MediatR;

namespace Application.StockPurchase.Queries.GetById;

public record GetStockPurchaseByIdQuery(int Id) : IRequest<BaseResponse<StockPurchaseResponse>>;
