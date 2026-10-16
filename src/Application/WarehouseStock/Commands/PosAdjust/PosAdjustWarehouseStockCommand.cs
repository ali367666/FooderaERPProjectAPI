using Application.Common.Responce;
using MediatR;

namespace Application.WarehouseStock.Commands.PosAdjust;

public record PosAdjustWarehouseStockCommand(PosAdjustWarehouseStockRequest Request) : IRequest<BaseResponse>;
