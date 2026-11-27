using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities.StoreMode;
using Domain.Enums;

namespace Application.StoreMode.Customers;

/// <summary>
/// Bonus that stayed unused longer than the company's expiry period is written off with an Expired ledger entry.
/// Spending is taken from the oldest bonus first, so only what is really left of the old bonus expires.
/// Runs when a customer's balance is looked at or used; nothing is changed when the period is 0 (never).
/// </summary>
public sealed class StoreBonusExpiryService
{
    private readonly IStoreCustomerRepository _customers;
    private readonly IStoreRepository _repository;

    public StoreBonusExpiryService(IStoreCustomerRepository customers, IStoreRepository repository)
    {
        _customers = customers;
        _repository = repository;
    }

    public async Task ExpireAsync(int companyId, int customerId, CancellationToken ct)
    {
        var settings = await _repository.GetOrCreateSettingsAsync(companyId, ct);
        if (settings.BonusExpiryDays <= 0) return;

        var entries = await _customers.GetBonusEntriesAsync(companyId, customerId, null, null, ct);
        var cutoff = DateTime.UtcNow.AddDays(-settings.BonusExpiryDays);

        // Lots in date order; every deduction (spent, reversed, expired, negative adjustment) eats the oldest lots first.
        var lots = new List<(DateTime At, decimal Left)>();
        var deductions = 0m;
        foreach (var e in entries)
        {
            if (e.Amount > 0) lots.Add((e.CreatedAtUtc, e.Amount));
            else deductions += -e.Amount;
        }

        for (var i = 0; i < lots.Count && deductions > 0; i++)
        {
            var take = Math.Min(deductions, lots[i].Left);
            lots[i] = (lots[i].At, lots[i].Left - take);
            deductions -= take;
        }

        var expired = lots.Where(l => l.At < cutoff).Sum(l => l.Left);
        if (expired <= 0) return;

        await _customers.AddBonusEntryAsync(new StoreBonusEntry
        {
            CompanyId = companyId,
            StoreCustomerId = customerId,
            Type = StoreBonusEntryType.Expired,
            Amount = -Math.Round(expired, 2, MidpointRounding.AwayFromZero),
            Note = $"{settings.BonusExpiryDays} gündən çox istifadə edilməyən bonusun müddəti bitdi",
        }, ct);
        await _repository.SaveChangesAsync(ct);
    }
}
