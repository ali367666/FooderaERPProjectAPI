using Application.Common.Responce;
using MediatR;

namespace Application.StockPurchase.Commands.Submit;

public record SubmitStockPurchaseCommand(int Id) : IRequest<BaseResponse>;
