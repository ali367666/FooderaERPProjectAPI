namespace Application.IdentityAdmin;

public class UpdateRoleRequest
{
    public string Name { get; set; } = default!;
    public bool RequiresRotatingPin { get; set; }
}
