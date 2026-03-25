using Application.Common.Responce;
using Application.StockCategory.Dtos.Request;
using Application.StockCategory.Dtos.Response;
using MediatR;

namespace Application.StockCategory.Commands.Update;

public record UpdateStockCategoryCommand(int Id, UpdateStockCategoryRequest Request)
    : IRequest<BaseResponse<StockCategoryResponse>>;