namespace Application.Auth.Dtos.Responce;

public sealed class LoginResponse
{
    public string Token { get; set; } = default!;
    public DateTime Expiration { get; set; }
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public IList<string> Roles { get; set; } = new List<string>();
}