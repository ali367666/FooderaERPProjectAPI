namespace Application.Auth.Dtos;

public class RevokeRefreshTokenRequest
{
    public string RefreshToken { get; set; } = default!;
}