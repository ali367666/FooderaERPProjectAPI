using Application.Common.Responce;
using Application.StockRequests.Dtos.Response;
using MediatR;

namespace Application.StockRequests.Queries.GetById;

public record GetStockRequestByIdQuery(int Id)
    : IRequest<BaseResponse<StockRequestResponse>>;