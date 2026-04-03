using Application.Common.Responce;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Delete;

public record DeleteWarehouseTransferCommand(int Id)
    : IRequest<BaseResponse>;