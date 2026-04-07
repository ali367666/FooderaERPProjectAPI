using Application.Common.Responce;
using Application.Position.Dtos;
using MediatR;

namespace Application.Positions.Queries.GetAll;

public record GetAllPositionsQuery() : IRequest<BaseResponse<List<PositionResponse>>>;