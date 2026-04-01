using Application.Common.Responce;
using Application.StockRequests.Dtos.Request;
using MediatR;

namespace Application.StockRequests.Commands.Create;

public record CreateStockRequestCommand(CreateStockRequestRequest Request)
    : IRequest<BaseResponse<int>>;