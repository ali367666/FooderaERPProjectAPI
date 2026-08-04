using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IDiscountRepository
{
    Task<List<Discount>> GetAllAsync(int companyId, CancellationToken ct);
    Task<Discount?> GetByIdAsync(int id, int companyId, CancellationToken ct);
    Task<Discount?> GetByCodeAsync(string code, int companyId, CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId, CancellationToken ct);
    Task AddAsync(Discount discount, CancellationToken ct);
    void Update(Discount discount);
    void Remove(Discount discount);
    Task SaveChangesAsync(CancellationToken ct);
}
