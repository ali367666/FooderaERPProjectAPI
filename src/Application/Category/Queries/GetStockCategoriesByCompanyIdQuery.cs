using Application.Common.Responce;
using Application.StockCategory.Dtos.Response;
using MediatR;

namespace Application.StockCategory.Queries.GetByCompanyId;

public record GetStockCategoriesByCompanyIdQuery(int CompanyId)
    : IRequest<BaseResponse<List<StockCategoryResponse>>>;