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

    private static readonly string[] PaymentBanks = ["Kapital", "Paşa", "ABB", "QR kod"];

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

        await _context.SaveChangesAsync(cancellationToken);
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
    /// Every company gets the fixed banks as counterparties in the "Bank" category. A counterparty the
    /// company already made with the same name is promoted to a bank instead of duplicated (names are unique).
    /// </summary>
    private async Task EnsurePaymentBanksAsync(int companyId, DateTime now, CancellationToken cancellationToken)
    {
        var category = await _context.CounterpartyCategories
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Name == "Bank", cancellationToken);
        if (category is null)
        {
            category = new CounterpartyCategory { CompanyId = companyId, Name = "Bank", IsActive = true, CreatedAtUtc = now };
            _context.CounterpartyCategories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var existing = await _context.Counterparties
            .Where(x => x.CompanyId == companyId && PaymentBanks.Contains(x.Name))
            .ToListAsync(cancellationToken);

        foreach (var name in PaymentBanks)
        {
            var counterparty = existing.FirstOrDefault(x => x.Name == name);
            if (counterparty is null)
            {
                _context.Counterparties.Add(new Counterparty
                {
                    CompanyId = companyId,
                    Name = name,
                    CategoryId = category.Id,
                    IsActive = true,
                    IsBank = true,
                    CreatedAtUtc = now
                });
            }
            else if (!counterparty.IsBank)
            {
                counterparty.IsBank = true;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
