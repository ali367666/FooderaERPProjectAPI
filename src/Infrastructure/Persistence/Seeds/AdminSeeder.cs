using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Identity;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(
        UserManager<User> userManager,
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
            return;

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

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.SuperAdmin);

        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(x => x.Description));
            throw new Exception($"Admin role could not be assigned: {errors}");
        }
    }
}