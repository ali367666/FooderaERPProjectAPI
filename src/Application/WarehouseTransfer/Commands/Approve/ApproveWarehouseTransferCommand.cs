using Application.Common.Responce;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Approve;

public record ApproveWarehouseTransferCommand(int Id) : IRequest<BaseResponse>;