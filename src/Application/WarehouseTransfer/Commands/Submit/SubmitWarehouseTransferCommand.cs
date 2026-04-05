using Application.Common.Responce;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Submit;

public record SubmitWarehouseTransferCommand(int Id) : IRequest<BaseResponse>;