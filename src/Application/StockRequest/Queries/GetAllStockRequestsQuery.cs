using Application.Common.Responce;
using Application.StockRequests.Dtos.Response;
using MediatR;

namespace Application.StockRequests.Queries.GetAll;

public record GetAllStockRequestsQuery(int? CompanyId = null)
    : IRequest<BaseResponse<List<StockRequestResponse>>>;