using Application.Common.Responce;
using Application.Warehouse.Dtos.Response;
using MediatR;

namespace Application.Warehouse.Queries.GetByRestaurantId;

public record GetWarehousesByRestaurantIdQuery(int RestaurantId)
    : IRequest<BaseResponse<List<WarehouseResponse>>>;