using Application.Common.Responce;
using Application.Restaurant.Dtos.Responce;
using MediatR;

namespace Application.Restaurant.Queries;

public record GetRestaurantByIdQuery(int Id) : IRequest<BaseResponse<RestaurantResponse>>;
