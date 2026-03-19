using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task<WebApplication> SeedIdentityAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        var companyId = await CompanySeeder.SeedDefaultCompanyAsync(context);
        await IdentitySeeder.SeedRolesAndPermissionsAsync(roleManager);
        await AdminSeeder.SeedAdminAsync(userManager, companyId);

        return app;
    }
}