using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Request;
using MediatR;

namespace Application.WarehouseStock.Commands.Update;

public record UpdateWarehouseStockCommand(int Id, UpdateWarehouseStockRequest Request)
    : IRequest<BaseResponse>;