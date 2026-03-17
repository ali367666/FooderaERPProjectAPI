namespace Application.User.Dtos.Responce;

public class CreateUserResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string UserName { get; set; } = default!;
}
