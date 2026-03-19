using Application.Auth.Dtos.Requests;
using Application.Auth.Dtos.Responce;
using Application.Common.Responce;
using MediatR;

namespace Application.Auth.Commands.Login;

public sealed record LoginCommand(LoginRequest Request)
    : IRequest<BaseResponse<LoginResponse>>;