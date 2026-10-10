namespace Application.IdentityAdmin;

public class PermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Module { get; set; } = default!;
    public string Action { get; set; } = default!;
}
