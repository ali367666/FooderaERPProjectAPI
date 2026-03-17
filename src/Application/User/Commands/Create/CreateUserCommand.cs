using Application.User.Dtos.Request;
using Application.User.Dtos.Responce;
using Domain.Enums;
using MediatR;

namespace Application.User.Commands.Create;


public record CreateUserCommand(CreateUserRequest dto)
    : IRequest<CreateUserResponse>;