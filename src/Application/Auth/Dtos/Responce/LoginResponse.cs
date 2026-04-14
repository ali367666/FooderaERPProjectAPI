namespace Application.Auth.Dtos.Responce;

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = default!;
    public DateTime AccessTokenExpiration { get; set; }

    public string RefreshToken { get; set; } = default!;
    public DateTime RefreshTokenExpiration { get; set; }
}