using Application.Common.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeds;

public class CompanyDefaultsSeeder : ICompanyDefaultsSeeder
{
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

        await EnsureCounterpartyCategoriesAsync(companyId, now, cancellationToken);
        await EnsurePaymentBanksAsync(companyId, now, cancellationToken);

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

    /// <summary>
    /// Every company has exactly the system's fixed counterparty categories. The old "Satış" and
    /// "Topdan" categories are folded into "Satıcı/Topdan": their counterparties move over first.
    /// </summary>
    private async Task EnsureCounterpartyCategoriesAsync(int companyId, DateTime now, CancellationToken cancellationToken)
    {
        var existing = await _context.CounterpartyCategories
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);

        foreach (var name in CounterpartyCategoryNames.Fixed)
        {
            var category = existing.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (category is null)
            {
                category = new CounterpartyCategory { CompanyId = companyId, Name = name, IsActive = true, CreatedAtUtc = now };
                _context.CounterpartyCategories.Add(category);
                existing.Add(category);
            }
            else if (!category.IsActive)
            {
                category.IsActive = true;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var wholesale = existing.First(x => string.Equals(x.Name, CounterpartyCategoryNames.Wholesale, StringComparison.OrdinalIgnoreCase));
        foreach (var legacyName in CounterpartyCategoryNames.Merged)
        {
            var legacy = existing.FirstOrDefault(x => string.Equals(x.Name, legacyName, StringComparison.OrdinalIgnoreCase));
            if (legacy is null)
                continue;

            var members = await _context.Counterparties
                .Where(c => c.CompanyId == companyId && c.CategoryId == legacy.Id)
                .ToListAsync(cancellationToken);
            foreach (var member in members)
                member.CategoryId = wholesale.Id;

            _context.CounterpartyCategories.Remove(legacy);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Only the "Bank" category is guaranteed. The banks themselves are given to a company by the SuperAdmin
    /// (<see cref="IPaymentBankProvisioner"/>); a new company starts without any, and the ones it already has stay as they are.
    /// </summary>
    private async Task EnsurePaymentBanksAsync(int companyId, DateTime now, CancellationToken cancellationToken)
    {
        var category = await _context.CounterpartyCategories
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Name == "Bank", cancellationToken);
        if (category is null)
        {
            category = new CounterpartyCategory { CompanyId = companyId, Name = "Bank", IsActive = true, CreatedAtUtc = now };
            _context.CounterpartyCategories.Add(category);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
