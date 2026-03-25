using Application.Common.Responce;
using Application.StockCategory.Dtos.Response;
using MediatR;

namespace Application.StockCategory.Queries.GetById;

public record GetStockCategoryByIdQuery(int Id)
    : IRequest<BaseResponse<StockCategoryDetailResponse>>;