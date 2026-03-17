using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.Seeds;

public static class IdentitySeed
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

        await SeedRolesAsync(roleManager);
        await SeedAdminAsync(userManager, dbContext);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
    {
        string[] roles = ["Admin", "User"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>
                {
                    Name = role,
                    NormalizedName = role.ToUpper()
                });
            }
        }
    }

    private static async Task SeedAdminAsync(
        UserManager<User> userManager,
        AppDbContext dbContext)
    {
        const string adminEmail = "admin@foodera.com";
        const string adminPassword = "Admin123!";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is not null)
            return;

        var company = await dbContext.Companies.FirstOrDefaultAsync();
        if (company is null)
            return;

        var adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Admin",
            WorkplaceType = EmployeeWorkplaceType.HeadOffice,
            CompanyId = company.Id,
            RestaurantId = null,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}