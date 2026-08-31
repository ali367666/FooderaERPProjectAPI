using Application.Auth.Dtos;
using Application.Auth.Dtos.Responce;
using Application.Common.Responce;
using MediatR;

namespace Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommand : IRequest<BaseResponse<LoginResponse>>
{
    public RefreshTokenRequest Request { get; set; } = default!;
    public string? IpAddress { get; set; }
}
