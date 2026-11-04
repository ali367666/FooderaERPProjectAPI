using Application.StoreMode.Customers;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Writes off store bonus that stayed unused past each company's bonus period, so balances in lists and reports are right
/// even for customers nobody opened. The same rule runs on demand when a balance is looked at or spent.
/// </summary>
public class StoreBonusExpiryWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StoreBonusExpiryWorker> _logger;

    public StoreBonusExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<StoreBonusExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the app finish starting (migrations, seeding) first.
        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Store bonus expiry run failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        List<(int CompanyId, int Days)> companies;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            companies = (await db.StoreSettings.AsNoTracking().Where(s => s.BonusExpiryDays > 0)
                .Select(s => new { s.CompanyId, s.BonusExpiryDays }).ToListAsync(ct))
                .Select(s => (s.CompanyId, s.BonusExpiryDays)).ToList();
        }

        foreach (var (companyId, days) in companies)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);
            List<int> customerIds;
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Only customers who earned bonus before the cutoff can have anything to expire.
                customerIds = await db.StoreBonusEntries.AsNoTracking()
                    .Where(e => e.CompanyId == companyId && e.Amount > 0 && e.CreatedAtUtc < cutoff)
                    .Select(e => e.StoreCustomerId).Distinct().ToListAsync(ct);
            }

            foreach (var customerId in customerIds)
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<StoreBonusExpiryService>().ExpireAsync(companyId, customerId, ct);
            }
        }
    }
}
