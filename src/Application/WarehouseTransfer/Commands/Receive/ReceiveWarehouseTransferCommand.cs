using Application.Common.Responce;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Receive;

public record ReceiveWarehouseTransferCommand(int Id) : IRequest<BaseResponse>;