using Application.Common.Responce;
using MediatR;

namespace Application.StockCategory.Commands.Delete;

public record DeleteStockCategoryCommand(int Id)
    : IRequest<BaseResponse>;