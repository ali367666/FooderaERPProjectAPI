using Domain.Entities;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    Task<User?> GetByCompanyAndCodeAsync(int companyId, string code, CancellationToken cancellationToken);

    Task<User?> GetByRfidCardIdAsync(string rfidCardId, CancellationToken cancellationToken);
}