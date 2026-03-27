using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using MediatR;

namespace Application.Warehouse.Queries.GetById;

public record GetWarehouseByIdQuery(int Id) : IRequest<BaseResponse<WarehouseResponse>>;