using Application.Common.Responce;
using MediatR;

namespace Application.StockRequests.Commands.Recall;

public record RecallStockRequestCommand(int Id) : IRequest<BaseResponse>;