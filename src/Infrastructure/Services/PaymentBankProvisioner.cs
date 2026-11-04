using Application.Common.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class PaymentBankProvisioner : IPaymentBankProvisioner
{
    private const string CategoryName = "Bank";
    private readonly AppDbContext _context;

    public PaymentBankProvisioner(AppDbContext context) => _context = context;

    public async Task<List<PaymentBankStatus>> GetAsync(int companyId, CancellationToken cancellationToken)
    {
        var active = (await _context.Counterparties.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.IsBank && x.IsActive)
                .Select(x => x.Name)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return PaymentBankCatalog.Names.Select(n => new PaymentBankStatus(n, active.Contains(n))).ToList();
    }

    public async Task SetAsync(int companyId, IReadOnlyCollection<string> enabledNames, CancellationToken cancellationToken)
    {
        var wanted = enabledNames
            .Select(n => PaymentBankCatalog.Names.FirstOrDefault(c => string.Equals(c, n?.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Where(n => n is not null)
            .Select(n => n!)
            .ToHashSet();

        var category = await _context.CounterpartyCategories
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Name == CategoryName, cancellationToken);
        if (category is null)
        {
            category = new CounterpartyCategory { CompanyId = companyId, Name = CategoryName, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
            _context.CounterpartyCategories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var existing = await _context.Counterparties
            .Where(x => x.CompanyId == companyId && PaymentBankCatalog.Names.Contains(x.Name))
            .ToListAsync(cancellationToken);

        foreach (var name in PaymentBankCatalog.Names)
        {
            var bank = existing.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            var on = wanted.Contains(name);
            if (bank is null)
            {
                if (on)
                    _context.Counterparties.Add(new Counterparty
                    {
                        CompanyId = companyId, Name = name, CategoryId = category.Id, IsActive = true, IsBank = true, CreatedAtUtc = DateTime.UtcNow,
                    });
                continue;
            }

            // A counterparty the company made itself with a bank's name becomes the bank when it is given.
            if (on) bank.IsBank = true;
            if (bank.IsBank) bank.IsActive = on;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
