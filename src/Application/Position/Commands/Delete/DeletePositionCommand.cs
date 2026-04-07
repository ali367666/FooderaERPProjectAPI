using Application.Common.Responce;
using MediatR;

namespace Application.Positions.Commands.Delete;

public record DeletePositionCommand(int Id) : IRequest<BaseResponse>;