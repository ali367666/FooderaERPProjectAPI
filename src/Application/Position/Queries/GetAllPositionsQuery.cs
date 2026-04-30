using Application.Common.Responce;
using Application.Position.Dtos;
using MediatR;

namespace Application.Positions.Queries.GetAll;

public record GetAllPositionsQuery(int CompanyId) : IRequest<BaseResponse<List<PositionResponse>>>;