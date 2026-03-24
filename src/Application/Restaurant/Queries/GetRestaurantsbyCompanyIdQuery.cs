using Application.Common.Responce;
using Application.Restaurant.Dtos.Responce;
using MediatR;

public record GetRestaurantsByCompanyIdQuery(int CompanyId)
    : IRequest<BaseResponse<List<RestaurantResponse>>>;