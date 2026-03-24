using Application.Common.Responce;
using Application.Restaurant.Dtos.Request;
using MediatR;

public record UpdateRestaurantCommand(int Id, UpdateRestaurantRequest Request)
    : IRequest<BaseResponse>;