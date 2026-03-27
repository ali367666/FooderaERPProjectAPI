using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using MediatR;

namespace Application.Warehouse.Queries.GetByDriverUserId;

public record GetWarehousesByDriverUserIdQuery(int DriverUserId)
    : IRequest<BaseResponse<List<WarehouseResponse>>>;