using Application.Common.Responce;
using MediatR;

namespace Application.StockRequests.Commands.Approve;

public record ApproveStockRequestCommand(int Id) : IRequest<BaseResponse>;