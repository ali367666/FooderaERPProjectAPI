using Application.Common.Responce;
using Application.StockCategory.Dtos.Response;
using MediatR;

namespace Application.StockCategory.Queries.GetChildrenByParentId;

public record GetChildrenByParentIdQuery(int ParentId)
    : IRequest<BaseResponse<List<StockCategoryResponse>>>;