using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

/// <summary>
/// Identity role, extended with tenant scoping. CompanyId is null for global/template roles
/// (the platform SuperAdmin role, and the seeded blueprint roles cloned for each new company);
/// it is set for every real, tenant-owned role so role names can be reused per company without
/// leaking permissions or user-role assignments across tenants.
/// </summary>
public class AppRole : IdentityRole<int>
{
    public int? CompanyId { get; set; }

    public AppRole() { }

    public AppRole(string roleName) : base(roleName) { }
}
