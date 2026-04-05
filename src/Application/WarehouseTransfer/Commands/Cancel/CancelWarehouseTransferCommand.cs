using Application.Common.Responce;
using MediatR;

namespace Application.WarehouseTransfer.Commands.Cancel;

public record CancelWarehouseTransferCommand(int Id) : IRequest<BaseResponse>;