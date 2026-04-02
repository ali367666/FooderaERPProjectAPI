using Application.Common.Responce;
using MediatR;

namespace Application.StockRequests.Commands.Reject;

public record RejectStockRequestCommand(int Id) : IRequest<BaseResponse>;