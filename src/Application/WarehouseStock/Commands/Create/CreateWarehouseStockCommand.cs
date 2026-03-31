using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Request;
using MediatR;

namespace Application.WarehouseStock.Commands.Create;

public record CreateWarehouseStockCommand(CreateWarehouseStockRequest Request)
    : IRequest<BaseResponse>;