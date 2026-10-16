namespace Application.IdentityAdmin;

public class UserRoleRowDto
{
    public int UserId { get; set; }
    public string UserFullName { get; set; } = default!;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = default!;
}
