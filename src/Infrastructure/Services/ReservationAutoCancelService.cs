using Application.Common.Helpers;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Every minute: a reservation (pending or confirmed) whose guest has not been seated within the
/// company's "avtomatik ləğv" window after its time is cancelled, which frees the table.
/// </summary>
public class ReservationAutoCancelService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationAutoCancelService> _logger;

    public ReservationAutoCancelService(IServiceScopeFactory scopeFactory, ILogger<ReservationAutoCancelService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the app finish starting (migrations, seeding) first.
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CancelOverdueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Reservation auto-cancel failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CancelOverdueAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var windows = await db.CompanySettings
            .AsNoTracking()
            .Where(s => s.ReservationAutoCancelMinutes > 0)
            .Select(s => new { s.CompanyId, s.ReservationAutoCancelMinutes })
            .ToListAsync(ct);
        if (windows.Count == 0)
            return;

        var now = BusinessTime.Now;
        var cancelled = 0;

        foreach (var window in windows)
        {
            var open = await db.Reservations
                .Where(r => r.CompanyId == window.CompanyId
                    && (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed)
                    && r.ReservationDate <= now.Date)
                .ToListAsync(ct);

            foreach (var reservation in open)
            {
                var start = reservation.ReservationDate.Date + reservation.ReservationTime;
                if (now < start.AddMinutes(window.ReservationAutoCancelMinutes))
                    continue;

                reservation.Status = ReservationStatus.Cancelled;
                var note = $"Avtomatik ləğv: {window.ReservationAutoCancelMinutes} dəq ərzində gəlmədi.";
                reservation.Note = string.IsNullOrWhiteSpace(reservation.Note) ? note : $"{reservation.Note} | {note}";
                cancelled++;
            }
        }

        if (cancelled > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("{Count} reservation(s) cancelled automatically (guest did not come).", cancelled);
        }
    }
}
