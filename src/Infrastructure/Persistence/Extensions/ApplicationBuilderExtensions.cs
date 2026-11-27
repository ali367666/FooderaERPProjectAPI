using Microsoft.Extensions.Logging;
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
        var defaultsSeeder = services.GetRequiredService<ICompanyDefaultsSeeder>();
        var configuration = services.GetRequiredService<IConfiguration>();

        // Local installations bring their database up to date on their own — nobody runs
        // "dotnet ef database update" on a restaurant's computer.
        if (configuration.GetValue<bool>("Database:MigrateOnStartup"))
            await context.Database.MigrateAsync();

        var companyId = await CompanySeeder.SeedDefaultCompanyAsync(context);
        await IdentitySeeder.SeedRolesAndPermissionsAsync(roleManager, context);
        await AdminSeeder.SeedAdminAsync(userManager, context, configuration, companyId);

        // Every existing company must have its own role set (no-ops once already provisioned) —
        // covers companies created before per-tenant roles existed, and any created outside the
        // normal CreateCompany flow.
        var allCompanyIds = await context.Companies.Select(c => c.Id).ToListAsync();
        foreach (var id in allCompanyIds)
        {
            await identityAdminService.CloneDefaultRolesForCompanyAsync(id);
            await defaultsSeeder.EnsureDefaultsAsync(id);

            // A role is the employee's job title: every role gets its position and every old position its role.
            // Never stops the start-up (the column may not exist yet when the migration is still pending).
            try
            {
                await identityAdminService.BackfillRolePositionsAsync(id);
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "Role/position sync skipped for company {CompanyId}", id);
            }
        }

        return app;
    }
}