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

    /// <summary>
    /// When true, POS login for users holding this role requires the daily-rotating 8-digit
    /// code (date prefix + the user's fixed 4-digit code) instead of the plain fixed 4-digit code.
    /// </summary>
    public bool RequiresRotatingPin { get; set; }

    public AppRole() { }

    public AppRole(string roleName) : base(roleName) { }
}
