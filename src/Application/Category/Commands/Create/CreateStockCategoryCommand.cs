using Application.Common.Responce;
using Application.StockCategory.Dtos.Request;
using Application.StockCategory.Dtos.Response;
using MediatR;

namespace Application.StockCategory.Commands.Create;

public record CreateStockCategoryCommand(CreateStockCategoryRequest Request)
    : IRequest<BaseResponse<CreateStockCategoryResponse>>;