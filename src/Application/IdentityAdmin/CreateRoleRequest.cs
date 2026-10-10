namespace Application.IdentityAdmin;

public class CreateRoleRequest
{
    public string Name { get; set; } = default!;

    /// <summary>
    /// Target company for the new role. Only honored for a SuperAdmin caller — a tenant Admin is
    /// always confined to their own company regardless of what this carries.
    /// </summary>
    public int? CompanyId { get; set; }

    public bool RequiresRotatingPin { get; set; }
}
