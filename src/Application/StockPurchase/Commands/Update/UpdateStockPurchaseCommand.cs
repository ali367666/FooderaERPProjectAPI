using Application.Common.Responce;
using Application.StockPurchase.Dtos.Request;
using MediatR;

namespace Application.StockPurchase.Commands.Update;

public record UpdateStockPurchaseCommand(int Id, UpdateStockPurchaseRequest Request)
    : IRequest<BaseResponse>;
