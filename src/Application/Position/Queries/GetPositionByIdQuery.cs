using Application.Common.Responce;
using Application.Position.Dtos;
using MediatR;

namespace Application.Positions.Queries.GetById;

public record GetPositionByIdQuery(int Id) : IRequest<BaseResponse<PositionResponse>>;