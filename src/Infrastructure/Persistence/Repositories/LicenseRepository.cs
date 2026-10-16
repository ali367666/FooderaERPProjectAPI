using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class LicenseRepository : ILicenseRepository
{
    private readonly AppDbContext _context;

    public LicenseRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<CompanyLicense?> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken) =>
        _context.CompanyLicenses.FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);

    public async Task<CompanyLicense> GetOrCreateAsync(int companyId, CancellationToken cancellationToken)
    {
        var license = await GetByCompanyIdAsync(companyId, cancellationToken);
        if (license is not null)
            return license;

        if (!await _context.Companies.AnyAsync(x => x.Id == companyId, cancellationToken))
            throw new Application.Common.Exceptions.NotFoundException("Şirkət tapılmadı.");

        license = new CompanyLicense { CompanyId = companyId, CreatedAtUtc = DateTime.UtcNow };
        await _context.CompanyLicenses.AddAsync(license, cancellationToken);
        return license;
    }

    public Task<List<LicensePayment>> GetPaymentsAsync(int companyId, CancellationToken cancellationToken) =>
        _context.LicensePayments
            .Include(x => x.CreatedByUser)
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddPaymentAsync(LicensePayment payment, CancellationToken cancellationToken) =>
        await _context.LicensePayments.AddAsync(payment, cancellationToken);

    public Task<List<TrustedDevice>> GetDevicesAsync(int companyId, CancellationToken cancellationToken) =>
        _context.TrustedDevices
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<TrustedDevice?> GetDeviceByIdAsync(int id, CancellationToken cancellationToken) =>
        _context.TrustedDevices.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<TrustedDevice?> GetActiveDeviceByKeyHashAsync(string keyHash, CancellationToken cancellationToken) =>
        _context.TrustedDevices.FirstOrDefaultAsync(x => x.KeyHash == keyHash && x.IsActive, cancellationToken);

    public async Task AddDeviceAsync(TrustedDevice device, CancellationToken cancellationToken) =>
        await _context.TrustedDevices.AddAsync(device, cancellationToken);

    public async Task AddRegistrationCodeAsync(DeviceRegistrationCode code, CancellationToken cancellationToken) =>
        await _context.DeviceRegistrationCodes.AddAsync(code, cancellationToken);

    public Task<DeviceRegistrationCode?> GetUsableRegistrationCodeAsync(
        string codeHash, DateTime utcNow, CancellationToken cancellationToken) =>
        _context.DeviceRegistrationCodes.FirstOrDefaultAsync(
            x => x.CodeHash == codeHash && x.UsedAtUtc == null && x.ExpiresAtUtc > utcNow,
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
