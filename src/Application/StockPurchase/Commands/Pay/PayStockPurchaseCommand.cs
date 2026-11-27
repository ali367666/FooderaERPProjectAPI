using Application.Common.Responce;
using Application.StockPurchase.Dtos.Request;
using MediatR;

namespace Application.StockPurchase.Commands.Pay;

/// <summary>Records a payment to the supplier against an approved purchase.</summary>
public record PayStockPurchaseCommand(int Id, PayStockPurchaseRequest Request) : IRequest<BaseResponse>;
