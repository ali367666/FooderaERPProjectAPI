using Application.Common.Responce;
using Application.StockPurchase.Dtos.Request;
using MediatR;

namespace Application.StockPurchase.Commands.Create;

public record CreateStockPurchaseCommand(CreateStockPurchaseRequest Request)
    : IRequest<BaseResponse<int>>;
