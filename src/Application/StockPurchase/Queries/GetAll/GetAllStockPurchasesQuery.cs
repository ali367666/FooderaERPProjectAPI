using Application.Common.Responce;
using Application.StockPurchase.Dtos.Response;
using MediatR;

namespace Application.StockPurchase.Queries.GetAll;

public record GetAllStockPurchasesQuery : IRequest<BaseResponse<List<StockPurchaseResponse>>>;
