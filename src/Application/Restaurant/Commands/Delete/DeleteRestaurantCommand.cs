using Application.Common.Responce;
using MediatR;

public record DeleteRestaurantCommand(int Id) : IRequest<BaseResponse>;