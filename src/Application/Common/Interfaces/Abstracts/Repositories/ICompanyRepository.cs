using Domain.Entities;
using System.Linq.Expressions;

public interface ICompanyRepository
{
    Task<List<Company>> GetAllAsync(CancellationToken cancellationToken);
    Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> AnyAsync(Expression<Func<Company, bool>> predicate, CancellationToken cancellationToken);
    Task AddAsync(Company company, CancellationToken cancellationToken);
    void Update(Company company);
    void Delete(Company company);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
}