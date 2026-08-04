using Application.Common.Responce;
using MediatR;

namespace Application.StockPurchase.Commands.Approve;

public record ApproveStockPurchaseCommand(int Id) : IRequest<BaseResponse>;
