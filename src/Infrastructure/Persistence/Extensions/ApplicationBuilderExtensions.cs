using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task<WebApplication> SeedIdentityAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var identityAdminService = services.GetRequiredService<IIdentityAdminService>();
        var configuration = services.GetRequiredService<IConfiguration>();

        var companyId = await CompanySeeder.SeedDefaultCompanyAsync(context);
        await IdentitySeeder.SeedRolesAndPermissionsAsync(roleManager, context);
        await AdminSeeder.SeedAdminAsync(userManager, configuration, companyId);

        // Every existing company must have its own role set (no-ops once already provisioned) —
        // covers companies created before per-tenant roles existed, and any created outside the
        // normal CreateCompany flow.
        var allCompanyIds = await context.Companies.Select(c => c.Id).ToListAsync();
        foreach (var id in allCompanyIds)
            await identityAdminService.CloneDefaultRolesForCompanyAsync(id);

        return app;
    }
}