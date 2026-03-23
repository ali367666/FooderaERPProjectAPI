using Application.Common.Responce;
using Application.Restaurant.Dtos.Request;
using Application.Restaurant.Dtos.Responce;
using MediatR;

namespace Application.Restaurant.Commands.Create;

public record CreateRestaurantCommand(CreateRestaurantRequest Request)
    : IRequest<BaseResponse<CreateRestaurantResponse>>;
