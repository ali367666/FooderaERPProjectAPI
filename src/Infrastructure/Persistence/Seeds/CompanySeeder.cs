using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

public static class CompanySeeder
{
    public static async Task<int> SeedDefaultCompanyAsync(AppDbContext context)
    {
        var existingCompany = await context.Companies
            .FirstOrDefaultAsync(x => x.CompanyCode == "FOODERA001");

        if (existingCompany is not null)
            return existingCompany.Id;

        var company = new Company
        {
            CompanyCode = "FOODERA001",
            Name = "Foodera Default Company",
            Description = "System default company for initial admin user",
            Address = "Baku, Azerbaijan",
            Country = Country.Azerbaijan,
            CountryCode = "+994",
            PrimaryPhoneNumber = "501234567",
            Email = "company@foodera.com"
        };

        await context.Companies.AddAsync(company);
        await context.SaveChangesAsync();

        return company.Id;
    }
}