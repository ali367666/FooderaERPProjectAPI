using Application.Common.Responce;
using MediatR;

namespace Application.StockPurchase.Commands.Delete;

public record DeleteStockPurchaseCommand(int Id) : IRequest<BaseResponse>;
