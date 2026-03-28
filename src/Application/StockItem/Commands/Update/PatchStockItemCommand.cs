using Application.Common.Responce;
using Application.StockItem.Dtos.Request;
using MediatR;

namespace Application.StockItem.Commands.Update;

public record PatchStockItemCommand(int Id, PatchStockItemRequest Request)
    : IRequest<BaseResponse>;