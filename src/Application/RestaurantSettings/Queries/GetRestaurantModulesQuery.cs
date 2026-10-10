using Application.Common.Responce;
using MediatR;

namespace Application.RestaurantSettings.Queries;

public record GetRestaurantModulesQuery(int RestaurantId) : IRequest<BaseResponse<RestaurantModulesResponse>>;
