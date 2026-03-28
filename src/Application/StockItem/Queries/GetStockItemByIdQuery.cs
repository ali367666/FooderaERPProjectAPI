using Application.Common.Responce;
using Application.StockItem.Dtos.Response;
using MediatR;

namespace Application.StockItem.Queries.GetById;

public record GetStockItemByIdQuery(int Id)
    : IRequest<BaseResponse<StockItemResponse>>;