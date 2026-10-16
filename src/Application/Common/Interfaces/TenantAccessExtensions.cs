namespace Application.Common.Interfaces;

/// <summary>
/// Tenant isolation helpers — every company sees and changes only its own data; the platform
/// SuperAdmin may act on any company.
/// </summary>
public static class TenantAccessExtensions
{
    /// <summary>True when the caller may read or change a record that belongs to <paramref name="companyId"/>.</summary>
    public static bool CanAccessCompany(this ICurrentUserService currentUser, int companyId) =>
        currentUser.IsSuperAdmin || (companyId > 0 && companyId == currentUser.CompanyId);

    /// <summary>
    /// The company a create/update acts on: a company sent in the request is honoured only for the
    /// SuperAdmin; everyone else always works in their own company, whatever the request says.
    /// </summary>
    public static int ResolveCompanyId(this ICurrentUserService currentUser, int? requestedCompanyId) =>
        currentUser.IsSuperAdmin && requestedCompanyId is > 0 ? requestedCompanyId.Value : currentUser.CompanyId;
}
