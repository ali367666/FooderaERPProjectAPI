using Application.Common.Responce;
using Application.Warehouse.Dtos.Request;
using Application.Warehouse.Dtos.Response;
using MediatR;

namespace Application.Warehouse.Commands.Create;

public record CreateWarehouseCommand(CreateWarehouseRequest Request)
    : IRequest<BaseResponse<WarehouseResponse>>;