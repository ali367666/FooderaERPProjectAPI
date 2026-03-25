using Application.Common.Responce;
using Application.StockCategory.Dtos.Request;
using Application.StockCategory.Dtos.Response;
using MediatR;

namespace Application.StockCategory.Queries.GetAll;

public record GetAllStockCategoriesQuery(GetAllStockCategoriesRequest Request)
    : IRequest<BaseResponse<List<StockCategoryResponse>>>;