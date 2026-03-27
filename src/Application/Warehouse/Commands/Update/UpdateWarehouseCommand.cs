using Application.Common.Responce;
using Application.Warehouse.Dtos.Request;
using MediatR;

namespace Application.Warehouse.Commands.Update;

public record UpdateWarehouseCommand(int Id, UpdateWarehouseRequest Request) : IRequest<BaseResponse>;