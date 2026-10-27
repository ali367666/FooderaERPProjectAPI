using Application.Common.Responce;
using MediatR;

namespace Application.StockPurchase.Commands.Cancel;

/// <summary>Cancels an already approved purchase: its quantities leave the warehouse again.</summary>
public record CancelStockPurchaseCommand(int Id) : IRequest<BaseResponse>;
