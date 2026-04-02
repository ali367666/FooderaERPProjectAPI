using Application.Common.Responce;
using MediatR;

namespace Application.StockRequests.Commands.Update;

public record UpdateStockRequestCommand(int Id, UpdateStockRequestRequest Request)
    : IRequest<BaseResponse>;