using System.Collections.Concurrent;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Services;

/// <summary>
/// Answers "may this request through?" for the Online/Offline gate. Runs on every authenticated
/// request, so licence state and device lookups are cached briefly and invalidated on change.
/// </summary>
public class DeviceAccessService : IDeviceAccessService
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LastSeenEvery = TimeSpan.FromMinutes(10);

    // Per-company version — bumping it makes every cached answer of that company stale at once.
    private static readonly ConcurrentDictionary<int, int> Versions = new();

    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IDeploymentInfo _deployment;

    public DeviceAccessService(AppDbContext context, IMemoryCache cache, IDeploymentInfo deployment)
    {
        _context = context;
        _cache = cache;
        _deployment = deployment;
    }

    public async Task<AccessDecision> CheckAsync(int companyId, string? deviceKey, CancellationToken cancellationToken)
    {
        var version = Versions.GetOrAdd(companyId, 0);

        // A Local installation is only reachable on the restaurant's own network, so there's no
        // device gate — instead the whole system runs only while its licence key is valid.
        if (_deployment.IsLocal)
        {
            var keyValid = await _cache.GetOrCreateAsync($"key:{companyId}:{version}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheFor;
                var license = await _context.CompanyLicenses.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
                return license?.IsLicenseKeyValid(DateTime.UtcNow) ?? false;
            });
            return keyValid ? AccessDecision.Allowed : AccessDecision.LicenseRequired;
        }

        var remoteActive = await _cache.GetOrCreateAsync($"lic:{companyId}:{version}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheFor;
            var license = await _context.CompanyLicenses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
            return license?.IsRemoteAccessActive(DateTime.UtcNow) ?? false;
        });

        if (remoteActive)
            return AccessDecision.Allowed;

        if (string.IsNullOrWhiteSpace(deviceKey))
            return AccessDecision.DeviceNotRegistered;

        var keyHash = SecretHash.Sha256Hex(deviceKey);
        var deviceOk = await _cache.GetOrCreateAsync($"dev:{companyId}:{version}:{keyHash}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheFor;
            var device = await _context.TrustedDevices
                .FirstOrDefaultAsync(x => x.KeyHash == keyHash && x.IsActive && x.CompanyId == companyId, cancellationToken);
            if (device is null)
                return false;

            var now = DateTime.UtcNow;
            if (device.LastSeenAtUtc is null || now - device.LastSeenAtUtc > LastSeenEvery)
            {
                device.LastSeenAtUtc = now;
                await _context.SaveChangesAsync(cancellationToken);
            }
            return true;
        });

        return deviceOk ? AccessDecision.Allowed : AccessDecision.DeviceNotRegistered;
    }

    public void Invalidate(int companyId) => Versions.AddOrUpdate(companyId, 1, (_, v) => v + 1);
}
