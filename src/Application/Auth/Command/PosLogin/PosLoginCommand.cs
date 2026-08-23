using Application.Auth.Dtos.Requests;
using Application.Auth.Dtos.Responce;
using Application.Common.Responce;
using MediatR;

namespace Application.Auth.Commands.PosLogin;

public sealed class PosLoginCommand : IRequest<BaseResponse<LoginResponse>>
{
    public PosLoginRequest Request { get; set; } = default!;
    public string? IpAddress { get; set; }
}
