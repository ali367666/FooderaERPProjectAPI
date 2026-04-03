using Application.Common.Responce;
using Application.WarehouseTransfers.Dtos.Request;
using MediatR;

namespace Application.WarehouseTransfers.Commands.Create;

public record CreateWarehouseTransferCommand(CreateWarehouseTransferRequest Request)
    : IRequest<BaseResponse<int>>;