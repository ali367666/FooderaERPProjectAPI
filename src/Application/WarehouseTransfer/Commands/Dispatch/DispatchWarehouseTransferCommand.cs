using Application.Common.Responce;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Dispatch;

public record DispatchWarehouseTransferCommand(int Id) : IRequest<BaseResponse>;