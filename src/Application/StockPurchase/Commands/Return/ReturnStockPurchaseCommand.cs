using Application.Common.Responce;
using Application.StockPurchase.Dtos.Request;
using MediatR;

namespace Application.StockPurchase.Commands.Return;

/// <summary>Returns goods of an approved purchase to the supplier: stock goes down, the cost and the debt shrink.</summary>
public record ReturnStockPurchaseCommand(int Id, ReturnStockPurchaseRequest Request) : IRequest<BaseResponse>;
