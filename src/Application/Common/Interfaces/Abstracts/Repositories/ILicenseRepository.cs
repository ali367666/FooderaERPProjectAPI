using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface ILicenseRepository
{
    Task<CompanyLicense?> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task<CompanyLicense> GetOrCreateAsync(int companyId, CancellationToken cancellationToken);

    Task<List<LicensePayment>> GetPaymentsAsync(int companyId, CancellationToken cancellationToken);
    Task AddPaymentAsync(LicensePayment payment, CancellationToken cancellationToken);

    Task<List<TrustedDevice>> GetDevicesAsync(int companyId, CancellationToken cancellationToken);
    Task<TrustedDevice?> GetDeviceByIdAsync(int id, CancellationToken cancellationToken);
    Task<TrustedDevice?> GetActiveDeviceByKeyHashAsync(string keyHash, CancellationToken cancellationToken);
    Task AddDeviceAsync(TrustedDevice device, CancellationToken cancellationToken);

    Task AddRegistrationCodeAsync(DeviceRegistrationCode code, CancellationToken cancellationToken);
    Task<DeviceRegistrationCode?> GetUsableRegistrationCodeAsync(string codeHash, DateTime utcNow, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
