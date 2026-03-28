using Application.Common.Responce;
using MediatR;

namespace Application.StockItem.Commands.Delete;

public record DeleteStockItemCommand(int Id) : IRequest<BaseResponse>;