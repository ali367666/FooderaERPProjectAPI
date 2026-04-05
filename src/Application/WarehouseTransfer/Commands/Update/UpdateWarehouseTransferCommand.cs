using Application.Common.Responce;
using Application.WarehouseTransfer.Dtos.Request;
using Application.WarehouseTransfers.Dtos.Request;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Update;

public record UpdateWarehouseTransferCommand(
    int Id,
    UpdateWarehouseTransferRequest Request
) : IRequest<BaseResponse>;