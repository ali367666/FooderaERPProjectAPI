using Application.Common.Responce;
using Application.StockItem.Dtos.Request;
using MediatR;

namespace Application.StockItem.Commands.Update;

public record UpdateStockItemCommand(int Id, StockItemRequest Request)
    : IRequest<BaseResponse>;