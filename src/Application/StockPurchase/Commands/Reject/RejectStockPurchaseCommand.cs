using Application.Common.Responce;
using MediatR;

namespace Application.StockPurchase.Commands.Reject;

public record RejectStockPurchaseCommand(int Id) : IRequest<BaseResponse>;
