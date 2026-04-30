using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IEmployeeRepository
{
    Task AddAsync(Employee employee, CancellationToken cancellationToken);
    Task<Employee?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Employee>> GetAllAsync(int companyId, CancellationToken cancellationToken);
    Task<List<Employee>> GetByPositionAsync(
        int companyId,
        int? positionId,
        string? positionName,
        CancellationToken cancellationToken);
    Task<bool> ExistsByUserIdAsync(int userId, CancellationToken cancellationToken);
    void Update(Employee employee);
    void Delete(Employee employee);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}