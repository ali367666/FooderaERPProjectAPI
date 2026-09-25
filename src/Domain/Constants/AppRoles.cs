namespace Domain.Constants;

public static class AppRoles
{
    /// <summary>
    /// Platform-level role. Not tied to any company (Role.CompanyId is null) and reserved for the
    /// reseller who manages every tenant. Never assignable from a tenant's own admin panel.
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Waiter = "Waiter";
    public const string Kitchen = "Kitchen";
    public const string Cashier = "Cashier";
    public const string User = "User";
}