using Application.Common.Responce;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Reject;

public record RejectWarehouseTransferCommand(int Id) : IRequest<BaseResponse>;