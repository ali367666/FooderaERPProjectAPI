using System.Security.Claims;
using System.Reflection;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedRolesAndPermissionsAsync(
        RoleManager<AppRole> roleManager,
        AppDbContext dbContext)
    {
        await NormalizePermissionNamesAsync(dbContext);

        var permissionConstants = typeof(AppPermissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.IsLiteral && !x.IsInitOnly && x.FieldType == typeof(string))
            .Select(x => (string)x.GetRawConstantValue()!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var permissionName in permissionConstants)
        {
            var permission = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Name == permissionName);
            if (permission is null)
            {
                var (module, action) = ParseModuleAction(permissionName);
                dbContext.Permissions.Add(new Permission
                {
                    Name = permissionName,
                    DisplayName = BuildDisplayName(module, action),
                    Module = module,
                    Action = action
                });
            }
        }
        await dbContext.SaveChangesAsync();

        // Default permission sets below are applied ONLY the first time a role is created.
        // Once a role exists, its permissions are owned entirely by the /dashboard/role-permissions
        // UI (UpdateRolePermissionsAsync) — re-applying the hardcoded list on every startup would
        // silently undo any permission an admin revoked through that screen.
        //
        // These are GLOBAL template roles (CompanyId = null) — they are never assigned to a user
        // directly. Every new company gets its own copy, cloned from these templates, so each
        // tenant's "Admin"/"Waiter"/etc. permissions can diverge without affecting other tenants.
        var roles = RolePermissionSeeder.Permissions.Keys.ToArray();

        foreach (var roleName in roles)
        {
            var role = await roleManager.Roles.FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant() && r.CompanyId == null);
            var isNewRole = role is null;

            if (role is null)
            {
                role = new AppRole(roleName) { CompanyId = null };
                dbContext.Roles.Add(role);
                await dbContext.SaveChangesAsync();
            }

            if (!isNewRole)
                continue;

            if (RolePermissionSeeder.Permissions.TryGetValue(roleName, out var permissions))
            {
                var permissionRows = await dbContext.Permissions
                    .Where(x => permissions.Contains(x.Name))
                    .ToListAsync();

                foreach (var row in permissionRows)
                {
                    dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = row.Id
                    });
                }

                foreach (var permission in permissions)
                {
                    await roleManager.AddClaimAsync(
                        role,
                        new Claim("Permission", permission));
                }
            }
        }

        await EnsureSuperAdminHasAllPermissionsAsync(roleManager, dbContext);
        await EnsureAdminTemplateHasBroadPermissionsAsync(roleManager, dbContext);

        await dbContext.SaveChangesAsync();
    }

    /// <summary>Permissions reserved for the platform SuperAdmin — never assignable to a tenant's own roles.</summary>
    private static readonly string[] PlatformOnlyPermissions =
    [
        Domain.Constants.AppPermissions.CompanyView,
        Domain.Constants.AppPermissions.CompanyCreate,
        Domain.Constants.AppPermissions.CompanyUpdate,
        Domain.Constants.AppPermissions.CompanyDelete,
    ];

    private static async Task NormalizePermissionNamesAsync(AppDbContext dbContext)
    {
        await NormalizePermissionAliasAsync(
            dbContext,
            oldName: "AuditLog.View",
            newName: "Permissions.AuditLog.View");
        await NormalizePermissionAliasAsync(
            dbContext,
            oldName: "AuditLogs.View",
            newName: "Permissions.AuditLog.View");

        await dbContext.SaveChangesAsync();
    }

    private static async Task NormalizePermissionAliasAsync(
        AppDbContext dbContext,
        string oldName,
        string newName)
    {
        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
            return;

        var oldPermission = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Name == oldName);
        if (oldPermission is null)
            return;

        var targetPermission = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Name == newName);
        if (targetPermission is null)
        {
            oldPermission.Name = newName;
            var (module, action) = ParseModuleAction(newName);
            oldPermission.Module = module;
            oldPermission.Action = action;
            oldPermission.DisplayName = BuildDisplayName(module, action);
        }
        else
        {
            var oldRoleMappings = await dbContext.RolePermissions
                .Where(x => x.PermissionId == oldPermission.Id)
                .ToListAsync();
            foreach (var rolePermission in oldRoleMappings)
            {
                var exists = await dbContext.RolePermissions.AnyAsync(x =>
                    x.RoleId == rolePermission.RoleId && x.PermissionId == targetPermission.Id);
                if (!exists)
                    rolePermission.PermissionId = targetPermission.Id;
                else
                    dbContext.RolePermissions.Remove(rolePermission);
            }

            dbContext.Permissions.Remove(oldPermission);
        }

        var roleClaims = await dbContext.RoleClaims
            .Where(x => x.ClaimType == "Permission" && x.ClaimValue == oldName)
            .ToListAsync();
        foreach (var claim in roleClaims)
            claim.ClaimValue = newName;

        var userClaims = await dbContext.UserClaims
            .Where(x => x.ClaimType == "Permission" && x.ClaimValue == oldName)
            .ToListAsync();
        foreach (var claim in userClaims)
            claim.ClaimValue = newName;
    }

    private static (string Module, string Action) ParseModuleAction(string permissionName)
    {
        var normalized = permissionName.StartsWith("Permissions.", StringComparison.OrdinalIgnoreCase)
            ? permissionName["Permissions.".Length..]
            : permissionName;

        var parts = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return (parts[^2], parts[^1]);

        if (parts.Length == 1)
            return (parts[0], "Access");

        return ("General", "Access");
    }

    private static string BuildDisplayName(string module, string action)
    {
        var moduleLabel = SplitCamelCase(module);
        var actionLabel = SplitCamelCase(action);
        return $"{actionLabel} {moduleLabel}";
    }

    private static string SplitCamelCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return string.Concat(value.Select((c, i) =>
            i > 0 && char.IsUpper(c) && !char.IsUpper(value[i - 1]) ? $" {c}" : c.ToString()));
    }

    /// <summary>The platform SuperAdmin (global, CompanyId = null) always has every permission.</summary>
    private static async Task EnsureSuperAdminHasAllPermissionsAsync(
        RoleManager<AppRole> roleManager,
        AppDbContext dbContext)
    {
        var superAdminRole = await roleManager.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == AppRoles.SuperAdmin.ToUpperInvariant() && r.CompanyId == null);
        if (superAdminRole is null)
        {
            superAdminRole = new AppRole(AppRoles.SuperAdmin) { CompanyId = null };
            dbContext.Roles.Add(superAdminRole);
            await dbContext.SaveChangesAsync();
        }

        var allPermissions = await dbContext.Permissions.AsNoTracking().ToListAsync();
        await GrantRolePermissionsAsync(roleManager, dbContext, superAdminRole, allPermissions);
    }

    /// <summary>
    /// The global "Admin" TEMPLATE (CompanyId = null, cloned for each new company) gets every
    /// permission except the platform-only ones — a tenant's own Admin can run their whole
    /// business but can never see or manage other companies.
    /// </summary>
    private static async Task EnsureAdminTemplateHasBroadPermissionsAsync(
        RoleManager<AppRole> roleManager,
        AppDbContext dbContext)
    {
        var adminTemplateRole = await roleManager.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == AppRoles.Admin.ToUpperInvariant() && r.CompanyId == null);
        if (adminTemplateRole is null)
        {
            adminTemplateRole = new AppRole(AppRoles.Admin) { CompanyId = null };
            dbContext.Roles.Add(adminTemplateRole);
            await dbContext.SaveChangesAsync();
        }

        var tenantPermissions = await dbContext.Permissions
            .AsNoTracking()
            .Where(x => !PlatformOnlyPermissions.Contains(x.Name))
            .ToListAsync();
        await GrantRolePermissionsAsync(roleManager, dbContext, adminTemplateRole, tenantPermissions);
    }

    private static async Task GrantRolePermissionsAsync(
        RoleManager<AppRole> roleManager,
        AppDbContext dbContext,
        AppRole role,
        List<Permission> permissions)
    {
        var existingPermissionIdsFromDb = await dbContext.RolePermissions
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.PermissionId)
            .ToListAsync();
        var existingPermissionIdsTracked = dbContext.ChangeTracker.Entries<RolePermission>()
            .Where(x => x.Entity.RoleId == role.Id && x.State != EntityState.Deleted)
            .Select(x => x.Entity.PermissionId)
            .ToList();
        var existingPermissionIds = existingPermissionIdsFromDb
            .Concat(existingPermissionIdsTracked)
            .Distinct()
            .ToHashSet();

        foreach (var permission in permissions.Where(x => !existingPermissionIds.Contains(x.Id)))
        {
            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            });
        }
        await dbContext.SaveChangesAsync();

        // Written directly to AspNetRoleClaims — RoleManager.AddClaimAsync re-validates the role's
        // name on every single call, which now legitimately fails (multiple companies share role
        // names) and silently drops the claim instead of persisting it.
        var existingValues = await dbContext.RoleClaims
            .Where(x => x.RoleId == role.Id && x.ClaimType == "Permission")
            .Select(x => x.ClaimValue!)
            .ToListAsync();
        var existingValueSet = existingValues.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in permissions.Where(x => !existingValueSet.Contains(x.Name)))
        {
            dbContext.RoleClaims.Add(new IdentityRoleClaim<int>
            {
                RoleId = role.Id,
                ClaimType = "Permission",
                ClaimValue = permission.Name
            });
        }
        await dbContext.SaveChangesAsync();
    }
}