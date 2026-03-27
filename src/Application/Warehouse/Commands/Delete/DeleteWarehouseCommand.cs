using Application.Common.Responce;
using MediatR;

namespace Application.Warehouse.Commands.Delete;

public record DeleteWarehouseCommand(int Id) : IRequest<BaseResponse>;