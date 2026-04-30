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
        RoleManager<IdentityRole<int>> roleManager,
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

        var roles = RolePermissionSeeder.Permissions.Keys.ToArray();

        foreach (var roleName in roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);

            if (role is null)
            {
                role = new IdentityRole<int>(roleName);
                await roleManager.CreateAsync(role);
            }

            var existingClaims = await roleManager.GetClaimsAsync(role);
            var existingRolePermissionIds = await dbContext.RolePermissions
                .Where(x => x.RoleId == role.Id)
                .Select(x => x.PermissionId)
                .ToListAsync();

            if (RolePermissionSeeder.Permissions.TryGetValue(roleName, out var permissions))
            {
                var permissionRows = await dbContext.Permissions
                    .Where(x => permissions.Contains(x.Name))
                    .ToListAsync();

                var expectedIds = permissionRows.Select(x => x.Id).ToHashSet();
                var staleMappings = await dbContext.RolePermissions
                    .Where(x => x.RoleId == role.Id && !expectedIds.Contains(x.PermissionId))
                    .ToListAsync();
                if (staleMappings.Count > 0)
                    dbContext.RolePermissions.RemoveRange(staleMappings);

                foreach (var row in permissionRows.Where(x => !existingRolePermissionIds.Contains(x.Id)))
                {
                    dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = row.Id
                    });
                }

                foreach (var permission in permissions)
                {
                    var exists = existingClaims.Any(c =>
                        c.Type == "Permission" && c.Value == permission);

                    if (!exists)
                    {
                        await roleManager.AddClaimAsync(
                            role,
                            new Claim("Permission", permission));
                    }
                }
            }
        }

        await EnsureAdminHasAllPermissionsAsync(roleManager, dbContext);

        await dbContext.SaveChangesAsync();
    }

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

    private static async Task EnsureAdminHasAllPermissionsAsync(
        RoleManager<IdentityRole<int>> roleManager,
        AppDbContext dbContext)
    {
        var adminRole = await roleManager.FindByNameAsync(AppRoles.Admin);
        if (adminRole is null)
        {
            adminRole = new IdentityRole<int>(AppRoles.Admin);
            await roleManager.CreateAsync(adminRole);
        }

        var allPermissions = await dbContext.Permissions.AsNoTracking().ToListAsync();
        var existingPermissionIdsFromDb = await dbContext.RolePermissions
            .Where(x => x.RoleId == adminRole.Id)
            .Select(x => x.PermissionId)
            .ToListAsync();
        var existingPermissionIdsTracked = dbContext.ChangeTracker.Entries<RolePermission>()
            .Where(x => x.Entity.RoleId == adminRole.Id && x.State != EntityState.Deleted)
            .Select(x => x.Entity.PermissionId)
            .ToList();
        var existingPermissionIds = existingPermissionIdsFromDb
            .Concat(existingPermissionIdsTracked)
            .Distinct()
            .ToHashSet();

        foreach (var permission in allPermissions.Where(x => !existingPermissionIds.Contains(x.Id)))
        {
            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id
            });
        }

        var existingClaims = await roleManager.GetClaimsAsync(adminRole);
        var existingValues = existingClaims
            .Where(x => x.Type == "Permission")
            .Select(x => x.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in allPermissions.Where(x => !existingValues.Contains(x.Name)))
            await roleManager.AddClaimAsync(adminRole, new Claim("Permission", permission.Name));
    }
}