using Application.Auth.Dtos.Requests;
using Application.Auth.Dtos.Responce;
using Application.Common.Responce;
using MediatR;

namespace Application.Auth.Commands.Login;

public sealed class LoginCommand : IRequest<BaseResponse<LoginResponse>>
{
    public LoginRequest Request { get; set; } = default!;
    public string? IpAddress { get; set; }
}