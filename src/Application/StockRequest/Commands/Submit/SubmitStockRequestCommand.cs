using Application.Common.Responce;
using MediatR;

namespace Application.StockRequests.Commands.Submit;

public record SubmitStockRequestCommand(int Id) : IRequest<BaseResponse>;