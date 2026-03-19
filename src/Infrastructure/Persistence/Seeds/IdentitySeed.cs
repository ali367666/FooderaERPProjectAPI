using System.Security.Claims;
using Domain.Constants;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedRolesAndPermissionsAsync(
        RoleManager<IdentityRole<int>> roleManager)
    {
        var roles = new[] { AppRoles.Admin, AppRoles.User };

        foreach (var roleName in roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);

            if (role is null)
            {
                role = new IdentityRole<int>(roleName);
                await roleManager.CreateAsync(role);
            }

            var existingClaims = await roleManager.GetClaimsAsync(role);

            if (RolePermissionSeeder.Permissions.TryGetValue(roleName, out var permissions))
            {
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
    }
}