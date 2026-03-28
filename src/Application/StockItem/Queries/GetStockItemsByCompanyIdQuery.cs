using Application.Common.Responce;
using Application.StockItem.Dtos.Response;
using MediatR;

namespace Application.StockItem.Queries.GetByCompanyId;

public record GetStockItemsByCompanyIdQuery(int CompanyId)
    : IRequest<BaseResponse<List<StockItemResponse>>>;