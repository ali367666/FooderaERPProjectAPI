using Application.QrMenu.Dtos;
using MediatR;

namespace Application.QrMenu.Queries;

public record GetQrMenuStatusQuery(int RestaurantId) : IRequest<QrMenuStatusResponse>;
