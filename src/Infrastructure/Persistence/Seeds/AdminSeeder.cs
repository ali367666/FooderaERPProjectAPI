using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(
        UserManager<User> userManager,
        int companyId)
    {
        const string email = "admin@foodera.com";
        const string username = "admin";
        const string password = "Admin123!";

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
            return;

        var user = new User
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            FullName = "System Administrator",
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

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Admin);

        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(x => x.Description));
            throw new Exception($"Admin role could not be assigned: {errors}");
        }
    }
}