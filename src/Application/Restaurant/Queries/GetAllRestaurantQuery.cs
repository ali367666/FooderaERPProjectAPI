using Application.Common.Responce;
using Application.Restaurant.Dtos.Responce;
using MediatR;

public record GetAllRestaurantsQuery(int? CompanyId = null) : IRequest<BaseResponse<List<RestaurantResponse>>>;