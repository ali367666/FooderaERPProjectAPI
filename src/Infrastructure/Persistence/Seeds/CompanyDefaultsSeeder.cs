using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeds;

public class CompanyDefaultsSeeder : ICompanyDefaultsSeeder
{
    private static readonly string[] CounterpartyCategories =
    [
        "Tədarükçü",
        "Əsas tədarükçü",
        "Pərakəndə",
        "Satış",
        "Topdan",
        "Bank",
        "Şöbə",
        "Personal"
    ];

    private static readonly string[] PrinterStationTypes =
    [
        "Kassa",
        "Mətbəx",
        "Qəlyan",
        "Salatxana",
        "Kababçı"
    ];

    private readonly AppDbContext _context;

    public CompanyDefaultsSeeder(AppDbContext context)
    {
        _context = context;
    }

    public async Task EnsureDefaultsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        if (!await _context.CounterpartyCategories.AnyAsync(x => x.CompanyId == companyId, cancellationToken))
        {
            await _context.CounterpartyCategories.AddRangeAsync(
                CounterpartyCategories.Select(name => new CounterpartyCategory
                {
                    CompanyId = companyId,
                    Name = name,
                    IsActive = true,
                    CreatedAtUtc = now
                }),
                cancellationToken);
        }

        if (!await _context.PrinterStationTypes.AnyAsync(x => x.CompanyId == companyId, cancellationToken))
        {
            await _context.PrinterStationTypes.AddRangeAsync(
                PrinterStationTypes.Select(name => new PrinterStationType
                {
                    CompanyId = companyId,
                    Name = name,
                    IsActive = true,
                    CreatedAtUtc = now
                }),
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
