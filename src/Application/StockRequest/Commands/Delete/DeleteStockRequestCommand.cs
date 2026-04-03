using Application.Common.Responce;
using MediatR;

namespace Application.StockRequests.Commands.Delete;

public record DeleteStockRequestCommand(int Id)
    : IRequest<BaseResponse>;