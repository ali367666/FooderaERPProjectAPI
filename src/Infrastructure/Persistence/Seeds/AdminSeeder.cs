using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Identity;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(
        UserManager<User> userManager,
        AppDbContext dbContext,
        IConfiguration configuration,
        int companyId)
    {
        var seedSection = configuration.GetSection("Seed");
        var username = seedSection["SuperAdminUserName"] ?? "admin";
        var email = seedSection["SuperAdminEmail"] ?? "admin@foodera.com";
        var password = seedSection["SuperAdminPassword"] ?? "Admin123!";
        var fullName = seedSection["SuperAdminFullName"] ?? "System Administrator";

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            // Self-heal: the platform SuperAdmin must never end up without its role (e.g. after its
            // user row was edited through the Users screen), or nobody can manage companies anymore.
            await EnsureSuperAdminRoleAsync(dbContext, existingUser.Id);
            return;
        }

        var user = new User
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            WorkplaceType = EmployeeWorkplaceType.HeadOffice,
            CompanyId = companyId,
            RestaurantId = null
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(x => x.Description));
            throw new Exception($"Admin user could not be seeded: {errors}");
        }

        await EnsureSuperAdminRoleAsync(dbContext, user.Id);
    }

    /// <summary>
    /// Assigns the global (CompanyId = null) SuperAdmin role straight through the database —
    /// UserManager.AddToRoleAsync looks roles up by name across every company and can miss or
    /// mis-pick it now that role names are only unique per company.
    /// </summary>
    private static async Task EnsureSuperAdminRoleAsync(AppDbContext dbContext, int userId)
    {
        var normalized = AppRoles.SuperAdmin.ToUpperInvariant();
        var role = await dbContext.Roles
            .Where(r => r.CompanyId == null && (r.NormalizedName == normalized || r.Name == AppRoles.SuperAdmin))
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync()
            ?? throw new Exception("The global SuperAdmin role was not found — role seeding must run first.");

        if (await dbContext.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.Id))
            return;

        dbContext.UserRoles.Add(new IdentityUserRole<int> { UserId = userId, RoleId = role.Id });
        await dbContext.SaveChangesAsync();
    }
}