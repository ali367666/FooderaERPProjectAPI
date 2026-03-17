using Application.Common.Responce;
using Application.User.Dtos.Request;
using Application.User.Dtos.Responce;
using Domain.Enums;
using MediatR;

namespace Application.User.Commands.Update;

public record UpdateUserCommand(int id,
    UpdateUserRequest dto
) : IRequest<BaseResponse<UpdateUserResponseDto>>;
