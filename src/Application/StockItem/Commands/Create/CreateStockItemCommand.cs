using Application.Common.Responce;
using Application.StockItem.Dtos.Request;
using MediatR;

namespace Application.StockItem.Commands.Create;

public record CreateStockItemCommand(StockItemRequest Request)
    : IRequest<BaseResponse<int>>;