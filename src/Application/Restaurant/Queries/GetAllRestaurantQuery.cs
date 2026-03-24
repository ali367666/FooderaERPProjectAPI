using Application.Common.Responce;
using Application.Restaurant.Dtos.Responce;
using MediatR;

public record GetAllRestaurantsQuery : IRequest<BaseResponse<List<RestaurantResponse>>>;