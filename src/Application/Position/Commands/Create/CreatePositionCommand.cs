using Application.Common.Responce;
using Application.Position.Dtos;
using MediatR;

namespace Application.Positions.Commands.Create;

public record CreatePositionCommand(CreatePositionRequest Request)
    : IRequest<BaseResponse<int>>;