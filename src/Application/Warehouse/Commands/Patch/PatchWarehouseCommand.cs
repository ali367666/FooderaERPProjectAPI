using Application.Common.Responce;
using Application.Warehouse.Dtos.Request;
using MediatR;

namespace Application.Warehouse.Commands.Patch;

public record PatchWarehouseCommand(int Id, PatchWarehouseRequest Request)
    : IRequest<BaseResponse>;
