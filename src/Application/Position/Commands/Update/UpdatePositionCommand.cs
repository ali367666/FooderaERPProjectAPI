using Application.Common.Responce;
using Application.Position.Dtos;
using MediatR;

namespace Application.Positions.Commands.Update;

public record UpdatePositionCommand(int Id, UpdatePositionRequest Request)
    : IRequest<BaseResponse>;