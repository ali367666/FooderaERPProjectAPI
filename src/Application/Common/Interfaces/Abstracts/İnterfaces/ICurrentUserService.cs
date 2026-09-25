namespace Application.Common.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }
    int CompanyId { get; }
    bool HasPermission(string permission);

    /// <summary>
    /// True only for the platform-level SuperAdmin (the reseller managing every tenant), never for
    /// a tenant's own "Admin" role. Backed by the SuperAdmin role claim baked into the JWT.
    /// </summary>
    bool IsSuperAdmin { get; }
}