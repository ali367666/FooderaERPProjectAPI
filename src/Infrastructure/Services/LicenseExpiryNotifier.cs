using Application.Common.Interfaces;
using Domain.Constants;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Hourly check of Online licences: 3 days left, 1 day left and on expiry, the platform
/// SuperAdmins and the company's Admins get an in-app notification (once per stage).
/// </summary>
public class LicenseExpiryNotifier : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LicenseExpiryNotifier> _logger;

    public LicenseExpiryNotifier(IServiceScopeFactory scopeFactory, ILogger<LicenseExpiryNotifier> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the app finish starting (migrations, seeding) first.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Licence expiry check failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CheckAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var soon = now.AddDays(3);
        var licenses = (await db.CompanyLicenses
                .Include(x => x.Company)
                .Where(x => (x.DeploymentType == Domain.Enums.DeploymentType.Local
                        && x.LicenseKeyExpiresAtUtc != null && x.LicenseKeyExpiresAtUtc <= soon)
                    || (x.DeploymentType != Domain.Enums.DeploymentType.Local && x.RemoteAccessEnabled
                        && x.RemoteAccessExpiresAtUtc != null && x.RemoteAccessExpiresAtUtc <= soon))
                .ToListAsync(ct))
            .Where(x => x.ReminderExpiresAtUtc is not null)
            .ToList();

        if (licenses.Count == 0)
            return;

        var superAdmins = await UsersInRoleAsync(db, AppRoles.SuperAdmin, null, ct);

        foreach (var license in licenses)
        {
            var daysLeft = (int)Math.Ceiling((license.ReminderExpiresAtUtc!.Value - now).TotalDays);
            var what = license.DeploymentType == Domain.Enums.DeploymentType.Local ? "Lisenziya açarının" : "Online lisenziyanın";
            var stage = daysLeft <= 0 ? 0 : daysLeft <= 1 ? 1 : 3;

            // Already told for this stage (or a later one).
            if (license.LastExpiryNoticeDays is { } last && last <= stage)
                continue;

            var companyName = license.Company?.Name ?? $"#{license.CompanyId}";
            var (title, companyMessage, adminMessage) = stage == 0
                ? ($"{what} vaxtı bitdi",
                    license.DeploymentType == Domain.Enums.DeploymentType.Local
                        ? "Lisenziya açarınızın vaxtı bitdi. Sistemdən istifadəyə davam etmək üçün yeni açar daxil edin."
                        : "Online (uzaqdan giriş) lisenziyanızın vaxtı bitdi. Sistemə yalnız qeydiyyatlı cihazlardan daxil olmaq mümkündür. Uzatmaq üçün bizimlə əlaqə saxlayın.",
                    $"{companyName}: {what} vaxtı bitdi.")
                : ($"{what} bitməsinə {daysLeft} gün qalıb",
                    $"{what} bitməsinə {daysLeft} gün qalıb. Kəsilməmək üçün ödənişi vaxtında edin.",
                    $"{companyName}: {what} bitməsinə {daysLeft} gün qalıb.");

            foreach (var (userId, userCompanyId) in await UsersInRoleAsync(db, AppRoles.Admin, license.CompanyId, ct))
                await notifications.CreateAsync(userId, userCompanyId, title, companyMessage, "LicenseExpiry",
                    license.Id, "CompanyLicense", cancellationToken: ct);

            foreach (var (userId, userCompanyId) in superAdmins)
                await notifications.CreateAsync(userId, userCompanyId, title, adminMessage, "LicenseExpiry",
                    license.Id, "CompanyLicense", cancellationToken: ct);

            license.LastExpiryNoticeDays = stage;
            _logger.LogInformation("Licence expiry notice sent. CompanyId: {CompanyId}, Stage: {Stage}", license.CompanyId, stage);
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Active users holding a role by name — per-company roles share names, so scope by company.</summary>
    private static async Task<List<(int UserId, int CompanyId)>> UsersInRoleAsync(
        AppDbContext db, string roleName, int? companyId, CancellationToken ct)
    {
        var normalized = roleName.ToUpperInvariant();
        return (await (from ur in db.UserRoles
                       join r in db.Roles on ur.RoleId equals r.Id
                       join u in db.Users on ur.UserId equals u.Id
                       where r.NormalizedName == normalized && u.IsActive
                             && (companyId == null || (r.CompanyId == companyId && u.CompanyId == companyId))
                       select new { u.Id, u.CompanyId })
                .Distinct()
                .ToListAsync(ct))
            .Select(x => (x.Id, x.CompanyId))
            .ToList();
    }
}
