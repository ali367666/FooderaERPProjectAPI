namespace Application.Auth.Dtos.Requests;

public sealed class LoginRequest
{
    public string EmailOrUserName { get; set; } = default!;
    public string Password { get; set; } = default!;
}
