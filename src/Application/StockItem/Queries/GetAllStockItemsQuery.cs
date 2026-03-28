using Application.Common.Responce;
using Application.StockItem.Dtos.Request;
using Application.StockItem.Dtos.Response;
using MediatR;

namespace Application.StockItem.Queries;

public record GetAllStockItemsQuery(GetAllStockItemsRequest Request)
    : IRequest<BaseResponse<List<StockItemResponse>>>;
